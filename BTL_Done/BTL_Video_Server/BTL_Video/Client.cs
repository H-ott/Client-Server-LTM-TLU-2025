using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BTL_Video
{
    public class Client
    {
        private readonly string _serverHost;
        private readonly int _serverPort;
        private TcpClient? _tcp;
        private StreamWriter? _writer;
        private StreamReader? _reader;
        private CancellationTokenSource? _cts;

        public event Action<string>? OnServerMessage; // raw line
        public event Action<string>? OnLog;
        public event Action<string[]?>? OnOnlineList;

        public string[]? LastOnlineList { get; set; }
        public string? DisplayName { get; private set; }
        public string? Username { get; set; }

        public Client(string serverHost = "127.0.0.1", int serverPort = 5000)
        {
            _serverHost = serverHost;
            _serverPort = serverPort;
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                _tcp = new TcpClient();
                await _tcp.ConnectAsync(_serverHost, _serverPort);
                var ns = _tcp.GetStream();
                _writer = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true };
                _reader = new StreamReader(ns, Encoding.UTF8);
                _cts = new CancellationTokenSource();
                _ = Task.Run(() => ListenLoop(_cts.Token));
                OnLog?.Invoke($"Connected to server {_serverHost}:{_serverPort}");
                return true;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Connect failed: {ex.Message}");
                return false;
            }
        }

        public async Task SendAsync(string line)
        {
            try
            {
                if (_writer != null)
                {
                    await _writer.WriteLineAsync(line);
                    OnLog?.Invoke($"TX: {line}");
                }
            }
            catch (Exception ex) { OnLog?.Invoke($"Send error: {ex.Message}"); }
        }

        private async Task ListenLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && _reader != null)
                {
                    var line = await _reader.ReadLineAsync();
                    if (line == null) break;
                    OnLog?.Invoke($"RX: {line}");

                    var parts = line.Split('|');

                    // cache ONLINE_LIST immediately
                    if (parts.Length > 0 && parts[0] == "ONLINE_LIST")
                    {
                        string[] list = Array.Empty<string>();
                        if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                            list = parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries);
                        LastOnlineList = list;
                        OnOnlineList?.Invoke(list);
                    }

                    // if login succeeded, proactively request online list (avoids race)
                    if (parts.Length > 0 && parts[0] == "OK")
                    {
                        DisplayName = parts.Length > 1 ? parts[1] : Username;
                        // request the current online list (server will reply with ONLINE_LIST)
                        try { await SendAsync("GET_ONLINE"); } catch { /* ignore */ }
                    }

                    OnServerMessage?.Invoke(line);
                }
            }
            catch (Exception ex)
            {
                // Provide full exception details and inspect inner SocketException if any
                if (ex is System.IO.IOException ioEx && ioEx.InnerException is System.Net.Sockets.SocketException sockEx)
                {
                    OnLog?.Invoke($"ListenLoop socket error: {sockEx.SocketErrorCode} - {sockEx.Message}");
                }
                else if (ex is System.Net.Sockets.SocketException sock)
                {
                    OnLog?.Invoke($"ListenLoop socket error: {sock.SocketErrorCode} - {sock.Message}");
                }
                else
                {
                    OnLog?.Invoke($"ListenLoop error: {ex}");
                }

                // graceful cleanup
                try { Disconnect(); } catch { }
            }
        }

        public void Disconnect()
        {
            try
            {
                _cts?.Cancel();
                _tcp?.Close();
            }
            catch { }
        }
    }
}