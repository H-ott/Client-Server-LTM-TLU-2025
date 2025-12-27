using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BTL_Video
{
    public partial class MainForm : Form
    {
        private readonly Client _client;
        private readonly string _username;
        private readonly string _displayName;

        public MainForm(Client client, string username, string displayName)
        {
            _client = client;
            _username = username;
            _displayName = displayName;
            InitializeComponent();

            _client.OnLog += s => Invoke(new Action(() => txtLog.AppendText(s + Environment.NewLine)));
            _client.OnServerMessage += Client_OnServerMessage;

            // subscribe to online-list with a marshaling wrapper to avoid type-mismatch when BeginInvoke marshals arguments
            _client.OnOnlineList += (list) =>
            {
                // marshal to UI thread using a parameterless Action closure (safe)
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => SetInitialOnlineList(list)));
                }
                else
                {
                    SetInitialOnlineList(list);
                }
            };
        }

        // New: allow setting an initial list (used right after login if a cached list exists)
        public void SetInitialOnlineList(string[]? list)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string[]?>(SetInitialOnlineList), list);
                return;
            }

            lstOnline.Items.Clear();
            if (list == null) return;
            foreach (var u in list)
            {
                if (u != _username) lstOnline.Items.Add(u);
            }
        }

        // Removed direct subscription method Client_OnOnlineList to avoid direct BeginInvoke of typed delegate
        private void Client_OnServerMessage(string line)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(Client_OnServerMessage), line); return; }
            var parts = line.Split('|');
            switch (parts[0])
            {
                case "ONLINE_LIST":
                    lstOnline.Items.Clear();
                    if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                    {
                        foreach (var u in parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (u != _username) lstOnline.Items.Add(u);
                        }
                    }
                    break;
                case "MSG":
                    {
                        // MSG|sender|receiver|content
                        var sender = parts.Length > 1 ? parts[1] : "unknown";
                        var content = parts.Length > 3 ? parts[3] : string.Join("|", parts.Skip(3));
                        txtChat.AppendText($"{sender}: {content}{Environment.NewLine}");
                        break;
                    }
                case "FILE_REQUEST":
                    {
                        // FILE_REQUEST|A|B|filename|size
                        var a = parts[1];
                        var filename = parts.Length > 3 ? parts[3] : "file";
                        var size = parts.Length > 4 ? parts[4] : "0";
                        var confirm = MessageBox.Show($"User {a} wants to send file {filename} ({size} bytes). Accept?", "File incoming", MessageBoxButtons.YesNo);
                        if (confirm == DialogResult.Yes)
                        {
                            // simply prepare receive buffer; file chunks will arrive as FILE_CHUNK messages in base64
                            // prompt save path
                            using var sfd = new SaveFileDialog() { FileName = filename };
                            if (sfd.ShowDialog() == DialogResult.OK)
                            {
                                _expectedFileStream = File.Create(sfd.FileName);
                                _expectedFileName = filename;
                                _expectedFileReceiver = a;
                                // send accept via protocol (reuse MSG or a dedicated message)
                                _ = _client.SendAsync($"MSG|{_username}|{a}|FILE_ACCEPT");
                            }
                        }
                        else
                        {
                            _ = _client.SendAsync($"MSG|{_username}|{a}|FILE_DECLINE");
                        }
                        break;
                    }
                case "FILE_CHUNK":
                    {
                        // FILE_CHUNK|A|B|filename|base64chunk
                        var a = parts[1];
                        var chunkB64 = parts.Length > 4 ? parts[4] : "";
                        if (_expectedFileReceiver == a && _expectedFileStream != null)
                        {
                            var data = Convert.FromBase64String(chunkB64);
                            _expectedFileStream.Write(data, 0, data.Length);
                        }
                        break;
                    }
                case "FILE_END":
                    {
                        var a = parts[1];
                        if (_expectedFileReceiver == a && _expectedFileStream != null)
                        {
                            _expectedFileStream?.Close();
                            _expectedFileStream = null;
                            MessageBox.Show($"File from {a} received: {_expectedFileName}");
                        }
                        break;
                    }
                case "CALL_REQUEST":
                    {
                        var a = parts[1];
                        var confirm = MessageBox.Show($"Incoming call from {a}. Accept?", "Call", MessageBoxButtons.YesNo);
                        if (confirm == DialogResult.Yes)
                        {
                            _ = _client.SendAsync($"CALL_ACCEPT|{_username}|{a}");
                        }
                        else
                        {
                            // no explicit decline protocol; you can add one
                        }
                        break;
                    }
                case "CALL_ACCEPT":
                    {
                        // CALL_ACCEPT|A|B|ipA|ipB|6000|6001
                        // CALL_ACCEPT|A|B|ipA|ipB|videoSendA|audioSendA|videoSendB|audioSendB
                        if (parts.Length >= 9)
                        {
                            var a = parts[1]; var b = parts[2];
                            var ipA = parts[3]; var ipB = parts[4];
                            var videoSendA = int.Parse(parts[5]);
                            var audioSendA = int.Parse(parts[6]);
                            var videoSendB = int.Parse(parts[7]);
                            var audioSendB = int.Parse(parts[8]);
                            string remoteIp;
                            int videoSend, audioSend, videoReceive, audioReceive;

                            if (_username == a)
                            {
                                // Caller
                                remoteIp = ipB;
                                videoSend = videoSendA;
                                audioSend = audioSendA;
                                videoReceive = videoSendB;
                                audioReceive = audioSendB;
                            }
                            else
                            {
                                // Receiver
                                remoteIp = ipA;
                                videoSend = videoSendB;
                                audioSend = audioSendB;
                                videoReceive = videoSendA;
                                audioReceive = audioSendA;
                            }
                            var vf = new VideoCallForm(remoteIp, videoSend, audioSend, videoReceive, audioReceive);
                            vf.Show();
                        }
                        break;
                    }
            }
        }

        private FileStream? _expectedFileStream;
        private string? _expectedFileName;
        private string? _expectedFileReceiver;

        private async void btnSendMsg_Click(object sender, EventArgs e)
        {
            if (lstOnline.SelectedItem == null) return;
            var to = lstOnline.SelectedItem.ToString()!;
            var content = txtMsg.Text.Trim();
            if (string.IsNullOrEmpty(content)) return;
            await _client.SendAsync($"MSG|{_username}|{to}|{content}");
            txtChat.AppendText($"Me -> {to}: {content}{Environment.NewLine}");
            txtMsg.Clear();
        }

        private async void btnSendFile_Click(object sender, EventArgs e)
        {
            if (lstOnline.SelectedItem == null) return;
            var to = lstOnline.SelectedItem.ToString()!;
            using var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;
            var fi = new FileInfo(ofd.FileName);
            await _client.SendAsync($"FILE_REQUEST|{_username}|{to}|{fi.Name}|{fi.Length}");
            // when receiver accepts they'll notify via MSG|...|FILE_ACCEPT and then we start sending chunks.
            // We'll listen to MSG notifications to start sending file
            txtLog.AppendText($"Sent file request to {to}{Environment.NewLine}");
        }

        private async void btnCall_Click(object sender, EventArgs e)
        {
            if (lstOnline.SelectedItem == null) return;
            var to = lstOnline.SelectedItem.ToString()!;
            await _client.SendAsync($"CALL_REQUEST|{_username}|{to}");
            txtLog.AppendText($"Calling {to}...{Environment.NewLine}");
        }

        // Hook to catch FILE_ACCEPT messages from receiver (we used MSG wrapper)
        private void txtLog_TextChanged(object sender, EventArgs e)
        {
            // not used
        }
    }
}