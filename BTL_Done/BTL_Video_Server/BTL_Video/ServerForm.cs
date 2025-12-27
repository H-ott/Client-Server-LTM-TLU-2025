using System;
using System.Linq;
using System.Windows.Forms;

namespace BTL_Video
{
    public partial class ServerForm : Form
    {
        private Server _server = new();

        public ServerForm()
        {
            InitializeComponent();
            _server.OnLog += Server_OnLog;
            _server.OnOnlineChanged += Server_OnOnlineChanged;

            // ensure server is stopped when form closes
            FormClosing += ServerForm_FormClosing;
        }

        private void ServerForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                if (_server != null && _server.IsRunning)
                {
                    _server.Stop();
                }
            }
            catch { }
        }

        private void Server_OnOnlineChanged()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(Server_OnOnlineChanged));
                return;
            }
            lstOnline.Items.Clear();
            foreach (var u in _server.GetOnlineUsers()) lstOnline.Items.Add(u);
        }

        private void Server_OnLog(string obj)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(Server_OnLog), obj);
                return;
            }
            txtLog.AppendText($"{DateTime.Now:HH:mm:ss} {obj}{Environment.NewLine}");
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            _server.Start();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            btnStop.Enabled = false;
            btnStart.Enabled = true;
            _server.Stop();
        }
    }
}