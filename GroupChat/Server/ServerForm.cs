using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ChatServerApp
{
    public partial class ServerForm : Form
    {
        private TcpListener? server;
        private Thread? listenThread;
        private List<TcpClient> clients = new List<TcpClient>();
        private Dictionary<TcpClient, string> userNames = new Dictionary<TcpClient, string>();
        private readonly object lockObj = new object();

        public ServerForm()
        {
            InitializeComponent();
            btnStart.Click += BtnStart_Click;
            this.FormClosing += ServerForm_FormClosing;
        }

        private void BtnStart_Click(object? sender, EventArgs e)
        {
            listenThread = new Thread(StartServer)
            {
                IsBackground = true
            };
            listenThread.Start();
            btnStart.Enabled = false;
            AppendLog("🚀 Server đang khởi động...");
        }

        private void StartServer()
        {
            server = new TcpListener(IPAddress.Any, 9000);
            server.Start();
            AppendLog("✅ Server đang lắng nghe trên cổng 9000...");

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                lock (lockObj) clients.Add(client);
                AppendLog("🔗 Client mới kết nối!");

                Thread clientThread = new Thread(HandleClient)
                {
                    IsBackground = true
                };
                clientThread.Start(client);
            }
        }

        private void HandleClient(object? obj)
        {
            TcpClient client = (TcpClient)obj!;
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;
            string username = "";

            try
            {
                // Nhận tên người dùng đầu tiên
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                username = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                AppendLog($"👤 {username} đã tham gia!");

                lock (lockObj)
                {
                    userNames[client] = username;
                }

                // Cập nhật danh sách người dùng cho tất cả client
                UpdateUserList();

                this.Invoke(new Action(() => listUser.Items.Add(username)));

                // Lắng nghe tin nhắn từ client
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string msgContent = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    string msgWithName = $"{username}: {msgContent}";
                    AppendLog(msgWithName);
                    Broadcast(msgWithName, client);
                }
            }
            catch
            {
                AppendLog($"⚠️ {username} đã thoát.");
            }
            finally
            {
                lock (lockObj)
                {
                    clients.Remove(client);
                    if (userNames.ContainsKey(client))
                        userNames.Remove(client);
                }

                UpdateUserList(); // Gửi danh sách người dùng sau khi rời
                client.Close();

                this.Invoke(new Action(() => listUser.Items.Remove(username)));
            }
        }

        // 🔹 Gửi danh sách người dùng hiện tại đến tất cả client
        private void UpdateUserList()
        {
            string userList = string.Join(",", userNames.Values);
            string message = "USERLIST:" + userList;
            byte[] data = Encoding.UTF8.GetBytes(message);

            lock (lockObj)
            {
                foreach (TcpClient c in clients)
                {
                    try
                    {
                        NetworkStream stream = c.GetStream();
                        stream.Write(data, 0, data.Length);
                    }
                    catch { }
                }
            }
        }

        // 🔹 Gửi tin nhắn chat thường
        private void Broadcast(string msg, TcpClient sender)
        {
            byte[] data = Encoding.UTF8.GetBytes(msg);
            lock (lockObj)
            {
                foreach (TcpClient c in clients)
                {
                    if (c != sender)
                    {
                        try
                        {
                            NetworkStream stream = c.GetStream();
                            stream.Write(data, 0, data.Length);
                        }
                        catch { }
                    }
                }
            }
        }

        private void AppendLog(string text)
        {
            this.Invoke(new Action(() =>
            {
                txtLog.AppendText($"{text}\r\n");
            }));
        }

        private void ServerForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                server?.Stop();
                listenThread?.Interrupt();
            }
            catch { }
        }
    }
}
