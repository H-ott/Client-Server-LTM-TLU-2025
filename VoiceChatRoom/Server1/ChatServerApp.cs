// ServerForm.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

namespace ChatServerApp
{
    public partial class ChatServerApp : Form
    {
        private TcpListener listener;
        private Thread acceptThread;
        private ConcurrentDictionary<TcpClient, ClientInfo> clients = new ConcurrentDictionary<TcpClient, ClientInfo>();
        private volatile bool running = false;

        public ChatServerApp()
        {
            InitializeComponent();
            btnStop.Enabled = false;
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (running) return;
            if (!int.TryParse(txtPort.Text.Trim(), out int port))
            {
                Log("Port không hợp lệ.");
                return;
            }

            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                running = true;
                acceptThread = new Thread(AcceptLoop) { IsBackground = true };
                acceptThread.Start();
                Log($"Server started on port {port}");
                btnStart.Enabled = false;
                btnStop.Enabled = true;
            }
            catch (Exception ex)
            {
                Log("Start error: " + ex.Message);
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            if (!running) return;
            running = false;

            try
            {
                try { listener.Stop(); } catch { }

                foreach (var kv in clients)
                {
                    try { kv.Key.Close(); } catch { }
                }
                clients.Clear();
            }
            catch (Exception ex) { Log("Stop error: " + ex.Message); }

            Log("Server stopped");
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }

        private void AcceptLoop()
        {
            try
            {
                while (running)
                {
                    TcpClient client;
                    try
                    {
                        client = listener.AcceptTcpClient();
                    }
                    catch
                    {
                        break; // listener stopped
                    }

                    Log("Incoming connection");
                    var t = new Thread(() => ClientHandler(client)) { IsBackground = true };
                    t.Start();
                }
            }
            catch (Exception ex)
            {
                Log("AcceptLoop ended: " + ex.Message);
            }
        }

        private void ClientHandler(TcpClient tcpClient)
        {
            NetworkStream stream = tcpClient.GetStream();
            var clientInfo = new ClientInfo { Tcp = tcpClient, Stream = stream, Username = "" };
            clients[tcpClient] = clientInfo;

            try
            {
                while (running && tcpClient.Connected)
                {
                    byte[] typeBuf = ReadExact(stream, 4);
                    if (typeBuf == null) break;
                    string type = Encoding.UTF8.GetString(typeBuf);

                    byte[] senderLenBuf = ReadExact(stream, 4);
                    if (senderLenBuf == null) break;
                    int senderLen = BitConverter.ToInt32(senderLenBuf, 0);

                    string sender = "";
                    if (senderLen > 0)
                    {
                        byte[] senderBuf = ReadExact(stream, senderLen);
                        if (senderBuf == null) break;
                        sender = Encoding.UTF8.GetString(senderBuf);
                    }

                    byte[] payloadLenBuf = ReadExact(stream, 4);
                    if (payloadLenBuf == null) break;
                    int payloadLen = BitConverter.ToInt32(payloadLenBuf, 0);

                    byte[] payload = payloadLen > 0 ? ReadExact(stream, payloadLen) : Array.Empty<byte>();
                    if (payloadLen > 0 && payload == null)
                    {
                        Log($"Payload missing or incomplete for {type} from {sender}");
                        break;
                    }

                    string trimmedType = type.Trim().ToUpperInvariant();

                    switch (trimmedType)
                    {
                        case "JOIN":
                            clientInfo.Username = sender;
                            Log($"User joined: {clientInfo.Username}");
                            UpdateClientList();
                            BroadcastUserList();
                            break;

                        case "LEAV":
                            Log($"User leaving: {clientInfo.Username}");
                            RemoveClient(tcpClient);
                            return;

                        case "MSG":
                            Log($"MSG from {sender}, size {payloadLen}");
                            BroadcastExcept("MSG", sender, payload, tcpClient);
                            break;

                        case "IMG":
                        case "FIL":
                        case "VOC":
                            Log($"{trimmedType} from {sender}, size {payloadLen}");
                            BroadcastExcept(trimmedType, sender, payload, tcpClient);
                            break;

                        default:
                            Log($"Unknown type from client: '{type}'");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Client handler error: " + ex.Message);
            }
            finally
            {
                RemoveClient(tcpClient);
            }
        }

        private void RemoveClient(TcpClient tcpClient)
        {
            if (clients.TryRemove(tcpClient, out var info))
            {
                try { tcpClient.Close(); } catch { }
                Log($"User left: {info.Username}");
                UpdateClientList();
                BroadcastUserList();
            }
        }

        private void UpdateClientList()
        {
            this.Invoke((Action)(() =>
            {
                lstClients.Items.Clear();
                foreach (var kv in clients)
                    lstClients.Items.Add(string.IsNullOrEmpty(kv.Value.Username) ? "(unknown)" : kv.Value.Username);
            }));
        }

        private void BroadcastUserList()
        {
            var list = new List<string>();
            foreach (var kv in clients)
            {
                if (!string.IsNullOrEmpty(kv.Value.Username))
                    list.Add(kv.Value.Username);
            }

            byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(list));
            Broadcast("USR ", "server", payload);
        }

        private void Broadcast(string type4, string senderName, byte[] payload)
        {
            foreach (var kv in clients)
            {
                try { SendFrame(kv.Value.Stream, type4, senderName, payload); } catch { }
            }
        }

        private void BroadcastExcept(string type4, string senderName, byte[] payload, TcpClient exceptClient)
        {
            foreach (var kv in clients)
            {
                if (kv.Key == exceptClient) continue;
                try { SendFrame(kv.Value.Stream, type4, senderName, payload); } catch { }
            }
        }

        private void SendFrame(NetworkStream stream, string type4, string senderName, byte[] payload)
        {
            if (stream == null) return;
            try
            {
                var t = type4.PadRight(4).Substring(0, 4);
                byte[] typeBytes = Encoding.UTF8.GetBytes(t);
                byte[] senderBytes = Encoding.UTF8.GetBytes(senderName ?? "");
                byte[] senderLen = BitConverter.GetBytes(senderBytes.Length);
                byte[] payloadLen = BitConverter.GetBytes(payload?.Length ?? 0);

                lock (stream)
                {
                    stream.Write(typeBytes, 0, 4);
                    stream.Write(senderLen, 0, 4);
                    if (senderBytes.Length > 0) stream.Write(senderBytes, 0, senderBytes.Length);
                    stream.Write(payloadLen, 0, 4);
                    if (payload != null && payload.Length > 0) stream.Write(payload, 0, payload.Length);
                    stream.Flush();
                }
            }
            catch { }
        }

        private byte[] ReadExact(NetworkStream stream, int count)
        {
            byte[] buf = new byte[count];
            int offset = 0;
            try
            {
                while (offset < count)
                {
                    int read = stream.Read(buf, offset, count - offset);
                    if (read == 0) return null;
                    offset += read;
                }
                return buf;
            }
            catch { return null; }
        }

        private void Log(string text)
        {
            this.Invoke((Action)(() =>
            {
                lstLog.Items.Add($"[{DateTime.Now:HH:mm:ss}] {text}");
                if (lstLog.Items.Count > 0) lstLog.TopIndex = lstLog.Items.Count - 1;
            }));
        }
    }

    public class ClientInfo
    {
        public TcpClient Tcp { get; set; }
        public NetworkStream Stream { get; set; }
        public string Username { get; set; }
    }
}
