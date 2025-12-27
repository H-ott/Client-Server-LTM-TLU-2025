using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BTL_Video
{
    public partial class LoginForm : Form
    {
        private Client? _client;

        public LoginForm()
        {
            InitializeComponent();
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            btnLogin.Enabled = false;
            var host = txtServer.Text.Trim();
            _client = new Client(host);
            _client.OnLog += s => Invoke(new Action(() => txtLog.AppendText(s + Environment.NewLine)));
            _client.OnServerMessage += Client_OnServerMessage;
            var ok = await _client.ConnectAsync();
            if (!ok)
            {
                btnLogin.Enabled = true;
                return;
            }

            _client.Username = txtUser.Text.Trim();
            await _client.SendAsync($"LOGIN|{txtUser.Text.Trim()}|{txtPass.Text.Trim()}");
        }

        private void Client_OnServerMessage(string line)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(Client_OnServerMessage), line); return; }
            var parts = line.Split('|');
            if (parts[0] == "OK")
            {
                var display = parts.Length > 1 ? parts[1] : txtUser.Text.Trim();
                var main = new MainForm(_client!, txtUser.Text.Trim(), display);

                // apply cached online list immediately if server already sent it
                if (_client!.LastOnlineList != null)
                {
                    main.SetInitialOnlineList(_client.LastOnlineList);
                }

                main.Show();
                Hide();
            }
            else if (parts[0] == "FAIL")
            {
                txtLog.AppendText($"Login failed: {(parts.Length > 1 ? parts[1] : "Unknown")}{Environment.NewLine}");
                btnLogin.Enabled = true;
            }
        }

        private async void btnRegister_Click(object sender, EventArgs e)
        {
            btnRegister.Enabled = false;
            var host = txtServer.Text.Trim();
            var client = new Client(host);
            client.OnLog += s => Invoke(new Action(() => txtLog.AppendText(s + Environment.NewLine)));

            client.OnServerMessage += (line) =>
            {
                if (InvokeRequired) { BeginInvoke(new Action<string>(HandleRegisterResponse), line); }
                else HandleRegisterResponse(line);
            };

            var connected = await client.ConnectAsync();
            if (!connected)
            {
                txtLog.AppendText("Register: connect failed." + Environment.NewLine);
                btnRegister.Enabled = true;
                return;
            }

            var user = txtUser.Text.Trim();
            var pass = txtPass.Text.Trim();
            var display = string.IsNullOrWhiteSpace(user) ? user : user;
            await client.SendAsync($"REGISTER|{user}|{pass}|{display}");

            void HandleRegisterResponse(string line)
            {
                var p = line.Split('|');
                if (p[0] == "OK")
                {
                    txtLog.AppendText("Registration successful. You can now log in." + Environment.NewLine);
                    try { client.Disconnect(); } catch { }
                }
                else if (p[0] == "FAIL")
                {
                    txtLog.AppendText($"Registration failed: {(p.Length > 1 ? p[1] : "Unknown")}{Environment.NewLine}");
                    try { client.Disconnect(); } catch { }
                }
                btnRegister.Enabled = true;
            }
        }
    }
}