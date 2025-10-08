using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ChatClientApp
{
    public partial class ClientForm : Form
    {
        private TcpClient client;
        private NetworkStream stream;
        private Thread listenThread;
        private bool connected = false;

        public ClientForm()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (!connected)
            {
                string name = txtName.Text.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Vui lòng nhập tên!");
                    return;
                }

                string ip = string.IsNullOrEmpty(txtServerIP.Text.Trim()) ? "127.0.0.1" : txtServerIP.Text.Trim();

                try
                {
                    client = new TcpClient(ip, 9000);
                    stream = client.GetStream();

                    // Gửi tên người dùng cho server
                    byte[] nameData = Encoding.UTF8.GetBytes(name);
                    stream.Write(nameData, 0, nameData.Length);

                    connected = true;
                    btnConnect.Text = "Ngắt kết nối";
                    lblStatus.Text = "🟢 Đã kết nối với server";
                    AppendChat("✅ Đã kết nối đến server!");

                    listenThread = new Thread(ListenFromServer)
                    {
                        IsBackground = true
                    };
                    listenThread.Start();
                }
                catch (Exception ex)
                {
                    AppendChat($"❌ Không thể kết nối: {ex.Message}");
                }
            }
            else
            {
                Disconnect();
            }
        }

        private void ListenFromServer()
        {
            try
            {
                byte[] buffer = new byte[1024];
                while (connected)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead <= 0) break;

                    string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    
                    // 🔹 Nếu server gửi danh sách người dùng
                    if (msg.StartsWith("USERLIST:"))
                    {
                        string list = msg.Substring(9); 
                        string[] users = list.Split(',');

                        this.Invoke(new Action(() =>
                        {
                            ListUsers.Items.Clear();
                            ListUsers.Items.Add("🧑‍🤝‍🧑 Người liên hệ:");
                            foreach (string user in users)
                            {
                                if (!string.IsNullOrWhiteSpace(user))
                                    ListUsers.Items.Add(user.Trim());
                            }
                        }));
                    }
                    else
                    {
                        AppendChat(msg); // Tin nhắn thường
                    }
                }
            }
            catch
            {
                AppendChat("⚠️ Mất kết nối với server.");
                lblStatus.Text = "🔴 Mất kết nối với server";
            }
            finally
            {
                Disconnect();
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (!connected) return;

            string msg = txtMessage.Text.Trim();
            if (msg == "") return;

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(msg);
                stream.Write(data, 0, data.Length);
                AppendChat($"Bạn: {msg}");
                txtMessage.Clear();
            }
            catch
            {
                AppendChat("❌ Gửi tin nhắn thất bại!");
            }
        }

        private void AppendChat(string text)
        {
            if (listChat.InvokeRequired)
                listChat.Invoke(new Action<string>(AppendChat), text);
            else
                listChat.Items.Add(text);
        }

        private void Disconnect()
        {
            connected = false;
            btnConnect.Text = "Kết nối";
            lblStatus.Text = "⚪ Đã ngắt kết nối khỏi server";

            try
            {
                stream?.Close();
                client?.Close();
            }
            catch { }

            AppendChat("🔴 Đã ngắt kết nối khỏi server.");
        }
    }
}
