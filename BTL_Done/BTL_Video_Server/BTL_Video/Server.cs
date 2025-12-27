using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace BTL_Video
{
    public class Account
    {
        public string username { get; set; } = "";

        // legacy plaintext password (kept for migration only)
        public string? password { get; set; }

        // new fields for secure storage
        public string? passwordHash { get; set; }   // base64 PBKDF2 hash
        public string? salt { get; set; }           // base64 salt
        public int iterations { get; set; } = 100_000;

        public string? displayName { get; set; }
    }

    public class Server
    {
        private int _port; // made non-readonly so we can fall back if port is in use
        private int _listeningPort; // actual port we're listening on
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private readonly string _accountsPath;
        private readonly object _accountsLock = new();
        private readonly ConcurrentDictionary<string, ClientConnection> _online = new();

        private List<Account> _accounts = new();

        private Task? _acceptLoopTask;
        private readonly object _startStopLock = new();

        public event Action<string>? OnLog;
        public event Action? OnOnlineChanged;

        private const int Pbkdf2Iterations = 100_000;
        private const int SaltSize = 16;   // bytes
        private const int HashBytes = 32;  // bytes

        public Server(int port = 5000)
        {
            _port = port;
            _listeningPort = port;
            _accountsPath = Path.Combine(AppContext.BaseDirectory, "accounts.json");
            LoadAccounts();
        }

        private void LoadAccounts()
        {
            try
            {
                if (File.Exists(_accountsPath))
                {
                    var json = File.ReadAllText(_accountsPath);
                    _accounts = JsonSerializer.Deserialize<List<Account>>(json) ?? new List<Account>();
                }
                else
                {
                    _accounts = new List<Account>();
                }

                // Migrate any legacy plaintext passwords to hashed storage.
                bool migrated = false;
                lock (_accountsLock)
                {
                    foreach (var acc in _accounts)
                    {
                        if (!string.IsNullOrEmpty(acc.password) && string.IsNullOrEmpty(acc.passwordHash))
                        {
                            try
                            {
                                var saltBytes = GenerateSalt(SaltSize);
                                var hashBytes = HashPassword(acc.password, saltBytes, Pbkdf2Iterations, HashBytes);
                                acc.salt = Convert.ToBase64String(saltBytes);
                                acc.passwordHash = Convert.ToBase64String(hashBytes);
                                acc.iterations = Pbkdf2Iterations;
                                acc.password = null; // clear legacy field
                                migrated = true;
                            }
                            catch (Exception ex)
                            {
                                OnLog?.Invoke($"Migration failed for user {acc.username}: {ex.Message}");
                            }
                        }
                    }

                    if (migrated)
                    {
                        try { SaveAccountsLocked(); OnLog?.Invoke("Migrated legacy plaintext passwords to PBKDF2 hashes."); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Failed to read accounts.json: {ex.Message}");
                _accounts = new List<Account>();
            }
        }

        private void SaveAccountsLocked()
        {
            // must be called inside lock(_accountsLock)
            var json = JsonSerializer.Serialize(_accounts, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_accountsPath, json);
        }

        private static byte[] GenerateSalt(int size)
        {
            var salt = new byte[size];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            return salt;
        }

        private static byte[] HashPassword(string password, byte[] salt, int iterations, int outputBytes)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(outputBytes);
        }

        private static bool VerifyPassword(string password, string base64Salt, string base64Hash, int iterations)
        {
            try
            {
                var salt = Convert.FromBase64String(base64Salt);
                var expected = Convert.FromBase64String(base64Hash);
                var actual = HashPassword(password, salt, iterations, expected.Length);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch { return false; }
        }

        public bool IsRunning
        {
            get
            {
                return _listener != null && _cts != null && !_cts.IsCancellationRequested;
            }
        }

        public void Start()
        {
            lock (_startStopLock)
            {
                if (IsRunning)
                {
                    OnLog?.Invoke("Server already running.");
                    return;
                }

                _cts = new CancellationTokenSource();

                // Try to bind to requested port; if port is in use, find a free port and use it.
                try
                {
                    _listener = new TcpListener(IPAddress.Any, _port);
                    _listener.Start();
                    _listeningPort = _port;
                    OnLog?.Invoke($"Server listening on TCP {_listeningPort}");
                }
                catch (SocketException se) when (se.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    OnLog?.Invoke($"Port {_port} is already in use. Attempting to find a free port...");
                    var fallback = FindFreePort();
                    if (fallback == 0)
                    {
                        OnLog?.Invoke("No free port found. Server not started.");
                        _cts = null;
                        return;
                    }

                    try
                    {
                        _listener = new TcpListener(IPAddress.Any, fallback);
                        _listener.Start();
                        _listeningPort = fallback;
                        OnLog?.Invoke($"Server listening on alternative TCP port {_listeningPort}");
                        OnLog?.Invoke($"NOTE: clients must connect to server using port {_listeningPort}");
                    }
                    catch (Exception ex)
                    {
                        OnLog?.Invoke($"Failed to start listener on fallback port {fallback}: {ex.Message}");
                        _cts = null;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke($"Failed to start listener: {ex.Message}");
                    _cts = null;
                    return;
                }

                // Start accept loop and keep the Task so Stop() can wait for it to complete.
                _acceptLoopTask = Task.Run(async () =>
                {
                    var ct = _cts.Token;
                    while (!ct.IsCancellationRequested)
                    {
                        try
                        {
                            var tcp = await _listener!.AcceptTcpClientAsync(ct);
                            _ = Task.Run(() => HandleNewTcpClient(tcp));
                        }
                        catch (OperationCanceledException) { break; }
                        catch (ObjectDisposedException) { break; }
                        catch (Exception ex) { OnLog?.Invoke($"Accept error: {ex.Message}"); await Task.Delay(50); }
                    }
                });
            }
        }

        public void Stop()
        {
            lock (_startStopLock)
            {
                try
                {
                    if (_cts == null && _listener == null && _online.IsEmpty)
                    {
                        OnLog?.Invoke("Server not running.");
                        return;
                    }

                    OnLog?.Invoke("Stopping server...");

                    try
                    {
                        _cts?.Cancel();
                    }
                    catch { }

                    try
                    {
                        // Stop accepting new clients
                        _listener?.Stop();
                    }
                    catch { }

                    // wait a short while for accept loop to finish
                    try
                    {
                        _acceptLoopTask?.Wait(1500);
                    }
                    catch { }

                    // Dispose and close all online connections
                    foreach (var kv in _online.ToArray())
                    {
                        try
                        {
                            kv.Value.Dispose();
                        }
                        catch { }
                    }

                    _online.Clear();

                    try { _listener = null; } catch { }
                    try { _cts?.Dispose(); } catch { }
                    _cts = null;
                    _acceptLoopTask = null;
                }
                catch (Exception ex) { OnLog?.Invoke($"Stop error: {ex.Message}"); }
                finally
                {
                    OnLog?.Invoke("Server stopped.");
                }
            }
        }

        private int FindFreePort()
        {
            // Ask OS for an available ephemeral port
            try
            {
                var l = new TcpListener(IPAddress.Loopback, 0);
                l.Start();
                var port = ((IPEndPoint)l.LocalEndpoint).Port;
                l.Stop();
                return port;
            }
            catch
            {
                return 0;
            }
        }

        private async Task HandleNewTcpClient(TcpClient tcp)
        {
            var remote = tcp.Client.RemoteEndPoint?.ToString() ?? "unknown";
            OnLog?.Invoke($"Client connected: {remote}");
            var conn = new ClientConnection(tcp, this, OnLog);
            try
            {
                await conn.ProcessAsync();
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Connection error ({remote}): {ex.Message}");
            }
            finally
            {
                // dispose connection (will also remove from online list if logged in)
                try { conn.Dispose(); } catch { }
            }
        }

        // Adds an online user - returns false if username already logged in
        internal bool AddOnline(string username, ClientConnection conn)
        {
            if (!_online.TryAdd(username, conn))
                return false;

            BroadcastOnlineList();
            OnOnlineChanged?.Invoke();
            return true;
        }

        // Remove online user
        internal void RemoveOnline(string? username)
        {
            if (username == null) return;
            if (_online.TryRemove(username, out var conn))
            {
                try { conn.Dispose(); } catch { }
            }
            BroadcastOnlineList();
            OnOnlineChanged?.Invoke();
        }

        internal ClientConnection? GetConnection(string username)
        {
            if (string.IsNullOrEmpty(username)) return null;
            _online.TryGetValue(username, out var c);
            return c;
        }

        private void BroadcastOnlineList()
        {
            var list = string.Join(",", _online.Keys.OrderBy(k => k));
            var dead = new List<string>();

            foreach (var kv in _online.ToArray())
            {
                try
                {
                    if (!kv.Value.TrySendRawLine($"ONLINE_LIST|{list}"))
                    {
                        dead.Add(kv.Key);
                    }
                }
                catch
                {
                    dead.Add(kv.Key);
                }
            }

            foreach (var d in dead)
            {
                if (_online.TryRemove(d, out var c))
                {
                    try { c.Dispose(); } catch { }
                }
            }

            OnLog?.Invoke($"Broadcast online list: {list}");
        }

        public string[] GetOnlineUsers() => _online.Keys.ToArray();

        // --- inner per-connection wrapper ---
        internal class ClientConnection : IDisposable
        {
            internal readonly TcpClient _tcp;
            private readonly Server _server;
            private readonly Action<string>? _onLog;
            private readonly NetworkStream _ns;
            private readonly StreamReader _reader;
            private readonly StreamWriter _writer;
            private readonly SemaphoreSlim _sendLock = new(1, 1);

            public string? Username { get; private set; }

            public ClientConnection(TcpClient tcp, Server server, Action<string>? onLog)
            {
                _tcp = tcp;
                _server = server;
                _onLog = onLog;
                _ns = _tcp.GetStream();
                _reader = new StreamReader(_ns, Encoding.UTF8, false, 4096, leaveOpen: true);
                _writer = new StreamWriter(_ns, Encoding.UTF8, 4096, leaveOpen: true) { AutoFlush = true };
            }

            // Thread-safe send; returns false if send fails
            public bool TrySendRawLine(string line)
            {
                try
                {
                    _sendLock.Wait();
                    _writer.WriteLine(line);
                    return true;
                }
                catch (Exception ex)
                {
                    _onLog?.Invoke($"Send to {Username ?? _tcp.Client.RemoteEndPoint?.ToString()} failed: {ex.Message}");
                    return false;
                }
                finally
                {
                    try { _sendLock.Release(); } catch { }
                }
            }

            public async Task ProcessAsync()
            {
                var remote = _tcp.Client.RemoteEndPoint?.ToString() ?? "unknown";
                try
                {
                    while (_tcp.Connected)
                    {
                        string? line;
                        try
                        {
                            line = await _reader.ReadLineAsync();
                        }
                        catch (IOException ioEx) when (ioEx.InnerException is SocketException sock)
                        {
                            _onLog?.Invoke($"Read socket error {sock.SocketErrorCode} - {sock.Message}");
                            break;
                        }
                        catch (ObjectDisposedException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            _onLog?.Invoke($"Read error: {ex.Message}");
                            break;
                        }

                        if (line == null) break;

                        _onLog?.Invoke($"RX [{remote}]: {line}");
                        var parts = line.Split('|');
                        var cmd = parts[0];

                        switch (cmd)
                        {
                            case "GET_ONLINE":
                                {
                                    var list = string.Join(",", _server._online.Keys.OrderBy(k => k));
                                    TrySendRawLine($"ONLINE_LIST|{list}");
                                    _onLog?.Invoke($"Sent ONLINE_LIST to {remote}: {list}");
                                    break;
                                }
                            case "REGISTER":
                                {
                                    var u = parts.Length > 1 ? parts[1] : "";
                                    var p = parts.Length > 2 ? parts[2] : "";
                                    var d = parts.Length > 3 ? parts[3] : u;
                                    if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(p))
                                    {
                                        TrySendRawLine("FAIL|Username and password required");
                                        break;
                                    }

                                    lock (_server._accountsLock)
                                    {
                                        if (_server._accounts.Any(a => a.username.Equals(u, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            TrySendRawLine("FAIL|Username already exists");
                                        }
                                        else
                                        {
                                            // create salt + pbkdf2 hash
                                            var saltBytes = GenerateSalt(SaltSize);
                                            var hashBytes = HashPassword(p, saltBytes, Pbkdf2Iterations, HashBytes);
                                            var acc = new Account
                                            {
                                                username = u,
                                                displayName = d,
                                                salt = Convert.ToBase64String(saltBytes),
                                                passwordHash = Convert.ToBase64String(hashBytes),
                                                iterations = Pbkdf2Iterations,
                                                password = null
                                            };
                                            _server._accounts.Add(acc);
                                            try
                                            {
                                                var json = JsonSerializer.Serialize(_server._accounts, new JsonSerializerOptions { WriteIndented = true });
                                                File.WriteAllText(_server._accountsPath, json);
                                                TrySendRawLine($"OK|{acc.displayName ?? acc.username}");
                                                _onLog?.Invoke($"Registered new user: {u}");
                                            }
                                            catch (Exception ex)
                                            {
                                                TrySendRawLine($"FAIL|Failed to save account: {ex.Message}");
                                            }
                                        }
                                    }
                                    break;
                                }
                            case "LOGIN":
                                {
                                    var u = parts.Length > 1 ? parts[1] : "";
                                    var p = parts.Length > 2 ? parts[2] : "";
                                    var acc = _server._accounts.FirstOrDefault(a => a.username == u);
                                    if (acc == null)
                                    {
                                        TrySendRawLine("FAIL|Tài khoản không tồn tại");
                                    }
                                    else
                                    {
                                        bool ok = false;
                                        // if account has hash, verify securely
                                        if (!string.IsNullOrEmpty(acc.passwordHash) && !string.IsNullOrEmpty(acc.salt))
                                        {
                                            ok = VerifyPassword(p, acc.salt!, acc.passwordHash!, acc.iterations);
                                        }
                                        else if (!string.IsNullOrEmpty(acc.password))
                                        {
                                            // legacy plaintext password stored — compare and migrate on success
                                            if (acc.password == p)
                                            {
                                                ok = true;
                                                // migrate: create salt+hash and clear plaintext
                                                try
                                                {
                                                    var saltBytes = GenerateSalt(SaltSize);
                                                    var hashBytes = HashPassword(p, saltBytes, Pbkdf2Iterations, HashBytes);
                                                    acc.salt = Convert.ToBase64String(saltBytes);
                                                    acc.passwordHash = Convert.ToBase64String(hashBytes);
                                                    acc.iterations = Pbkdf2Iterations;
                                                    acc.password = null;
                                                    lock (_server._accountsLock)
                                                    {
                                                        _server.SaveAccountsLocked();
                                                    }
                                                    _onLog?.Invoke($"Migrated legacy password for user {u} to PBKDF2 hash.");
                                                }
                                                catch (Exception ex)
                                                {
                                                    _onLog?.Invoke($"Failed to migrate password for user {u}: {ex.Message}");
                                                }
                                            }
                                        }

                                        if (!ok)
                                        {
                                            TrySendRawLine("FAIL|Sai mật khẩu");
                                        }
                                        else
                                        {
                                            Username = u;
                                            if (!_server.AddOnline(Username, this))
                                            {
                                                TrySendRawLine("FAIL|Already logged in");
                                                Username = null;
                                            }
                                            else
                                            {
                                                TrySendRawLine($"OK|{acc.displayName ?? acc.username}");
                                                _onLog?.Invoke($"{Username} logged in from {remote}");
                                            }
                                        }
                                    }
                                    break;
                                }
                            case "MSG":
                                {
                                    // MSG|sender|receiver|content
                                    if (parts.Length >= 4)
                                    {
                                        var receiver = parts[2];
                                        var rc = _server.GetConnection(receiver);
                                        if (rc != null)
                                        {
                                            rc.TrySendRawLine(line);
                                            _onLog?.Invoke($"Forwarded message to {receiver}");
                                        }
                                    }
                                    break;
                                }
                            case "FILE_REQUEST":
                                {
                                    if (parts.Length >= 5)
                                    {
                                        var b = parts[2];
                                        var rc = _server.GetConnection(b);
                                        if (rc != null) rc.TrySendRawLine(line);
                                    }
                                    break;
                                }
                            case "FILE_CHUNK":
                                {
                                    if (parts.Length >= 5)
                                    {
                                        var b = parts[2];
                                        var rc = _server.GetConnection(b);
                                        if (rc != null) rc.TrySendRawLine(line);
                                    }
                                    break;
                                }
                            case "FILE_END":
                                {
                                    if (parts.Length >= 4)
                                    {
                                        var b = parts[2];
                                        var rc = _server.GetConnection(b);
                                        if (rc != null) rc.TrySendRawLine(line);
                                    }
                                    break;
                                }
                            case "CALL_REQUEST":
                                {
                                    if (parts.Length >= 3)
                                    {
                                        var b = parts[2];
                                        var rc = _server.GetConnection(b);
                                        if (rc != null) rc.TrySendRawLine(line);
                                    }
                                    break;
                                }
                            case "CALL_ACCEPT":
                                {
                                    if (parts.Length >= 3)
                                    {
                                        var accepter = parts[1];
                                        var other = parts[2];
                                        var tcpB = _server.GetConnection(accepter);
                                        var tcpA = _server.GetConnection(other);
                                        if (tcpA != null && tcpB != null)
                                        {
                                            var ipA = ((IPEndPoint)tcpA._tcp.Client.RemoteEndPoint!).Address.ToString();
                                            var ipB = ((IPEndPoint)tcpB._tcp.Client.RemoteEndPoint!).Address.ToString();
                                            var callInfo = $"CALL_ACCEPT|{other}|{accepter}|{ipA}|{ipB}|6000|6001|6002|6003";
                                            tcpA.TrySendRawLine(callInfo);
                                            tcpB.TrySendRawLine(callInfo);
                                            _onLog?.Invoke($"Brokering CALL between {other} and {accepter}: {ipA} <-> {ipB}");
                                        }
                                    }
                                    break;
                                }
                            case "LOGOUT":
                                {
                                    _server.RemoveOnline(Username);
                                    Username = null;
                                    break;
                                }
                            default:
                                _onLog?.Invoke($"Unknown cmd: {cmd}");
                                break;
                        }
                    }
                }
                finally
                {
                    if (Username != null)
                    {
                        _server.RemoveOnline(Username);
                        Username = null;
                    }
                }
            }

            public void Dispose()
            {
                try
                {
                    // attempt graceful socket shutdown then close
                    try
                    {
                        if (_tcp?.Client != null && _tcp.Client.Connected)
                        {
                            try { _tcp.Client.Shutdown(SocketShutdown.Both); } catch { }
                        }
                    }
                    catch { }

                    _writer?.Flush();
                }
                catch { }
                try { _reader?.Dispose(); } catch { }
                try { _writer?.Dispose(); } catch { }
                try { _ns?.Dispose(); } catch { }
                try { _tcp?.Close(); } catch { }
                try { _sendLock?.Dispose(); } catch { }
            }
        }
    }
}