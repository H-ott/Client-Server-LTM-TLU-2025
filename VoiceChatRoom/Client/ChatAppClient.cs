// ClientForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using NAudio.Wave;

namespace ChatAppClient
{
    public partial class ClientForm : Form
    {
        private TcpClient? client;
        private NetworkStream? stream;
        private Thread? listenThread;
        private volatile bool running = false;
        private string username = "";

        // NAudio: ghi âm + phát
        private WaveInEvent? waveIn;
        private WaveFileWriter? waveWriter;
        private readonly string tempVoicePath = Path.Combine(Path.GetTempPath(), "voice_temp.wav");
        private WaveOutEvent? activeOutput;

        public ClientForm()
        {
            InitializeComponent();
            // trạng thái UI khởi tạo
            btnStopVoice.Enabled = false;
        }

        // ---------------- Connect / Disconnect ----------------
        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (client != null && client.Connected)
            {
                Disconnect();
                return;
            }

            username = txtUsername.Text.Trim();
            string ip = txtServerIp.Text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Nhập username trước khi kết nối.");
                return;
            }

            try
            {
                client = new TcpClient();
                client.Connect(ip, ChatProtocol.PORT);
                stream = client.GetStream();

                running = true;
                listenThread = new Thread(ListenLoop) { IsBackground = true };
                listenThread.Start();

                // Send JOIN (no payload)
                SendPacket("JOIN", username, Array.Empty<byte>());

                btnConnect.Text = "Ngắt kết nối";
                AddMessage("✅ Kết nối thành công!");
            }
            catch (Exception ex)
            {
                AddMessage("❌ Kết nối thất bại: " + ex.Message);
                Disconnect();
            }
        }

        private void Disconnect()
        {
            try
            {
                if (client != null && client.Connected)
                {
                    SendPacket("LEAV", username, Array.Empty<byte>());
                }
            }
            catch { /* ignore */ }

            StopVoiceCapture();

            try
            {
                running = false;
                stream?.Close();
                client?.Close();
            }
            catch { /* ignore */ }

            btnConnect.Text = "Kết nối";
            AddMessage("🔌 Đã ngắt kết nối.");
        }

        // ---------------- Listen loop ----------------
        private void ListenLoop()
        {
            try
            {
                while (running && stream != null)
                {
                    byte[]? typeBuf = ReadExact(stream, 4);
                    if (typeBuf == null) break;
                    string type = Encoding.UTF8.GetString(typeBuf);

                    byte[]? nameLenBuf = ReadExact(stream, 4);
                    if (nameLenBuf == null) break;
                    int nameLen = BitConverter.ToInt32(nameLenBuf, 0);

                    string sender = "";
                    if (nameLen > 0)
                    {
                        byte[]? nameBuf = ReadExact(stream, nameLen);
                        if (nameBuf == null) break;
                        sender = Encoding.UTF8.GetString(nameBuf);
                    }

                    byte[]? payloadLenBuf = ReadExact(stream, 4);
                    if (payloadLenBuf == null) break;
                    int payloadLen = BitConverter.ToInt32(payloadLenBuf, 0);

                    byte[] payload = payloadLen > 0 ? ReadExact(stream, payloadLen) ?? Array.Empty<byte>() : Array.Empty<byte>();

                    string t = type.Trim();

                    switch (t)
                    {
                        case "MSG":
                            {
                                string text = Encoding.UTF8.GetString(payload);
                                AddChatBubble(sender, text, true);
                                break;
                            }
                        case "IMG":
                            {
                                TryShowImage(sender, payload);
                                break;
                            }
                        case "FIL":
                            {
                                HandleIncomingFileRaw(sender, payload);
                                break;
                            }
                        case "VOC":
                            {
                                HandleIncomingVoice(sender, payload);
                                break;
                            }
                        case "USR":
                            {
                                try
                                {
                                    string json = Encoding.UTF8.GetString(payload);
                                    var users = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                                    UpdateUserList(users);
                                }
                                catch (Exception ex)
                                {
                                    AddMessage("Lỗi parse danh sách user: " + ex.Message);
                                }
                                break;
                            }
                        default:
                            AddMessage($"❔ Gói tin không xác định: {t}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                AddMessage("⚠️ Mất kết nối: " + ex.Message);
            }
            finally
            {
                Disconnect();
            }
        }

        // ---------------- Send packet (framing) ----------------
        // Frame: TYPE(4) | senderLen(4) | senderBytes | payloadLen(4) | payloadBytes
        private void SendPacket(string type4, string senderName, byte[] payload)
        {
            try
            {
                if (stream == null || client == null || !client.Connected) return;
                string t4 = type4.PadRight(4).Substring(0, 4);
                byte[] typeBytes = Encoding.UTF8.GetBytes(t4);
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
            catch (Exception ex)
            {
                AddMessage("⚠️ Gửi lỗi: " + ex.Message);
            }
        }

        private byte[]? ReadExact(NetworkStream ns, int count)
        {
            byte[] buf = new byte[count];
            int offset = 0;
            try
            {
                while (offset < count)
                {
                    int r = ns.Read(buf, offset, count - offset);
                    if (r == 0) return null; // remote closed
                    offset += r;
                }
                return buf;
            }
            catch
            {
                return null;
            }
        }

        // ---------------- Voice (record / send / play) ----------------
        private void btnStartVoice_Click(object sender, EventArgs e)
        {
            try
            {
                if (File.Exists(tempVoicePath)) File.Delete(tempVoicePath);

                waveIn = new WaveInEvent();
                waveIn.WaveFormat = new WaveFormat(16000, 16, 1);
                waveWriter = new WaveFileWriter(tempVoicePath, waveIn.WaveFormat);

                waveIn.DataAvailable += (s, a) =>
                {
                    try { waveWriter?.Write(a.Buffer, 0, a.BytesRecorded); waveWriter?.Flush(); }
                    catch { }
                };

                waveIn.RecordingStopped += (s, a) =>
                {
                    try { waveWriter?.Dispose(); waveWriter = null; waveIn?.Dispose(); waveIn = null; }
                    catch { }
                };

                waveIn.StartRecording();
                btnStartVoice.Enabled = false;
                btnStopVoice.Enabled = true;
                AddMessage("🎙 Bắt đầu ghi âm...");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi ghi âm: " + ex.Message);
            }
        }

        private void btnStopVoice_Click(object sender, EventArgs e)
        {
            StopVoiceCapture();

            if (File.Exists(tempVoicePath))
            {
                try
                {
                    byte[] voiceData = File.ReadAllBytes(tempVoicePath);
                    SendPacket("VOC", username, voiceData);
                    AddMessage("📤 Đã gửi voice message.");
                    AddOutgoingVoiceBubble(voiceData);
                }
                catch (Exception ex)
                {
                    AddMessage("Lỗi gửi voice: " + ex.Message);
                }
            }
        }

        private void StopVoiceCapture()
        {
            try { waveIn?.StopRecording(); } catch { }
            try { waveWriter?.Dispose(); } catch { }
            waveIn = null;
            waveWriter = null;
            btnStartVoice.Enabled = true;
            btnStopVoice.Enabled = false;
        }

        private void HandleIncomingVoice(string sender, byte[] data)
        {
            // hiển thị nút Play trong chat; phát trực tiếp khi nhấn
            if (InvokeRequired)
            {
                Invoke(new Action(() => HandleIncomingVoice(sender, data)));
                return;
            }

            Panel p = new Panel
            {
                AutoSize = true,
                Padding = new Padding(6),
                Margin = new Padding(6),
                BackColor = Color.FromArgb(240, 240, 255)
            };

            Label lSender = new Label
            {
                Text = $"{sender} (Voice)",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            Button btnPlay = new Button
            {
                Text = "▶️ Phát",
                Width = 80,
                Height = 28,
                Top = lSender.Bottom + 6,
                Left = 5
            };

            btnPlay.Click += (s, e) =>
            {
                PlayAudioFromBytes(data, btnPlay);
            };

            p.Controls.Add(lSender);
            p.Controls.Add(btnPlay);
            flowMessages.Controls.Add(p);
            flowMessages.ScrollControlIntoView(p);
        }




        // ---------------- Send text / image / file handlers ----------------
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (stream == null || client == null || !client.Connected) { AddMessage("❌ Chưa kết nối."); return; }
            string msg = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;
            SendPacket("MSG", username, Encoding.UTF8.GetBytes(msg));
            AddChatBubble("You", msg, false);
            txtMessage.Clear();
        }

        private void btnSendImage_Click(object sender, EventArgs e)
        {
            if (stream == null) { AddMessage("Chưa kết nối."); return; }
            using OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif";
            if (ofd.ShowDialog() != DialogResult.OK) return;

            byte[] imgBytes = File.ReadAllBytes(ofd.FileName);
            SendPacket("IMG", username, imgBytes);

            using var img = Image.FromFile(ofd.FileName);
            AddChatImage("You", (Image)img.Clone(), false);
        }

        private void btnSendFile_Click(object sender, EventArgs e)
        {
            if (stream == null) { AddMessage("Chưa kết nối."); return; }
            using OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "All files|*.*";
            if (ofd.ShowDialog() != DialogResult.OK) return;

            byte[] fileBytes = File.ReadAllBytes(ofd.FileName);
            string fname = Path.GetFileName(ofd.FileName);
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms, Encoding.UTF8);
            byte[] fnameBytes = Encoding.UTF8.GetBytes(fname);
            bw.Write(fnameBytes.Length);
            bw.Write(fnameBytes);
            bw.Write(fileBytes);
            bw.Flush();
            SendPacket("FIL", username, ms.ToArray());
            AddChatFile("You", fname, fileBytes.Length, false, fileBytes);
        }

        // ---------------- UI helpers ----------------
        private void AddMessage(string text)
        {
            if (InvokeRequired) { Invoke(new Action<string>(AddMessage), text); return; }
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}\r\n");
        }

        private void AddChatBubble(string sender, string text, bool incoming)
        {
            if (InvokeRequired) { Invoke(new Action<string, string, bool>(AddChatBubble), sender, text, incoming); return; }
            Panel p = new Panel
            {
                AutoSize = true,
                Padding = new Padding(8),
                Margin = new Padding(6),
                BackColor = incoming ? Color.FromArgb(235, 245, 255) : Color.FromArgb(230, 255, 230),
                MaximumSize = new Size(flowMessages.Width - 30, 0),
            };
            Label lSender = new Label { Text = sender, AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            Label lText = new Label { Text = text, AutoSize = true, MaximumSize = new Size(p.MaximumSize.Width - 20, 0), Font = new Font("Segoe UI", 10F) };
            lText.Top = lSender.Bottom + 6;
            p.Controls.Add(lSender);
            p.Controls.Add(lText);
            flowMessages.Controls.Add(p);
            flowMessages.ScrollControlIntoView(p);
        }

        private void TryShowImage(string sender, byte[] data)
        {
            try
            {
                using var ms = new MemoryStream(data);
                var img = Image.FromStream(ms);
                if (InvokeRequired) { Invoke(new Action(() => AddChatImage(sender, (Image)img.Clone(), true))); }
                else AddChatImage(sender, (Image)img.Clone(), true);
                img.Dispose();
            }
            catch (Exception ex)
            {
                AddMessage("Lỗi hiển thị ảnh: " + ex.Message);
            }
        }

        private void AddChatImage(string sender, Image img, bool incoming)
        {
            if (InvokeRequired) { Invoke(new Action(() => AddChatImage(sender, img, incoming))); return; }

            Panel p = new Panel { AutoSize = true, Padding = new Padding(6), Margin = new Padding(6), BackColor = Color.White };
            Label lSender = new Label { Text = sender, AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            PictureBox pic = new PictureBox { Image = img, SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(240, 160), Cursor = Cursors.Hand };
            pic.Top = lSender.Bottom + 6;

            pic.Click += (s, e) =>
            {
                using SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap|*.bmp";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string ext = Path.GetExtension(sfd.FileName).ToLower();
                        switch (ext)
                        {
                            case ".jpg":
                            case ".jpeg": img.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Jpeg); break;
                            case ".bmp": img.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Bmp); break;
                            default: img.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png); break;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi lưu ảnh: " + ex.Message);
                    }
                }
            };

            p.Controls.Add(lSender);
            p.Controls.Add(pic);
            flowMessages.Controls.Add(p);
            flowMessages.ScrollControlIntoView(p);
        }

        private void AddChatFile(string sender, string fileName, int size, bool incoming, byte[]? fileData = null)
        {
            if (InvokeRequired) { Invoke(new Action(() => AddChatFile(sender, fileName, size, incoming, fileData))); return; }

            Panel p = new Panel { AutoSize = true, Padding = new Padding(6), Margin = new Padding(6), BackColor = Color.WhiteSmoke };
            Label lSender = new Label { Text = sender, AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            LinkLabel link = new LinkLabel { Text = $"{fileName} ({size / 1024} KB)", AutoSize = true, LinkColor = Color.Blue };
            link.Top = lSender.Bottom + 6;

            link.Click += (s, e) =>
            {
                using SaveFileDialog sfd = new SaveFileDialog();
                sfd.FileName = fileName;
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    if (fileData != null)
                        File.WriteAllBytes(sfd.FileName, fileData);
                    else
                        MessageBox.Show("File dữ liệu không có sẵn để lưu.");
                }
            };

            p.Controls.Add(lSender);
            p.Controls.Add(link);
            flowMessages.Controls.Add(p);
            flowMessages.ScrollControlIntoView(p);
        }

        // payload chứa: 4 bytes filenameLen | filename bytes | file bytes
        private void HandleIncomingFileRaw(string sender, byte[] payload)
        {
            try
            {
                using var ms = new MemoryStream(payload);
                using var br = new BinaryReader(ms, Encoding.UTF8);
                int fnameLen = br.ReadInt32();
                string fname = Encoding.UTF8.GetString(br.ReadBytes(fnameLen));
                byte[] fileBytes = br.ReadBytes((int)(ms.Length - ms.Position));
                AddChatFile(sender, fname, fileBytes.Length, true, fileBytes);
            }
            catch (Exception ex)
            {
                AddMessage("Lỗi xử lý file: " + ex.Message);
            }
        }

        // play WAV bytes (expects WAV format)
        private void PlayAudioFromBytes(byte[] audioBytes, Button? btnToUpdate = null)
        {
            try
            {
                // stop previous
                try { activeOutput?.Stop(); activeOutput?.Dispose(); activeOutput = null; } catch { }

                var ms = new MemoryStream(audioBytes);
                var reader = new WaveFileReader(ms);
                var output = new WaveOutEvent();
                activeOutput = output;
                output.Init(reader);
                output.Play();

                if (btnToUpdate != null)
                {
                    btnToUpdate.Text = "🔊 Đang phát";
                    btnToUpdate.Enabled = false;
                }

                output.PlaybackStopped += (s, e) =>
                {
                    try { reader.Dispose(); ms.Dispose(); output.Dispose(); } catch { }
                    activeOutput = null;
                    if (btnToUpdate != null)
                    {
                        btnToUpdate.Invoke(new Action(() =>
                        {
                            btnToUpdate.Text = "▶️ Phát";
                            btnToUpdate.Enabled = true;
                        }));
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi phát âm thanh: " + ex.Message);
                if (btnToUpdate != null)
                {
                    btnToUpdate.Text = "▶️ Phát";
                    btnToUpdate.Enabled = true;
                }
            }
        }

        private void AddOutgoingVoiceBubble(byte[] data)
        {
            if (InvokeRequired) { Invoke(new Action<byte[]>(AddOutgoingVoiceBubble), data); return; }

            Panel p = new Panel { AutoSize = true, Padding = new Padding(6), Margin = new Padding(6), BackColor = Color.FromArgb(230, 255, 230) };
            Label lSender = new Label { Text = "You (Voice)", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            Button btnPlay = new Button { Text = "▶️ Nghe lại", Width = 90, Height = 28, Top = lSender.Bottom + 6, Left = 5 };
            btnPlay.Click += (s, e) => PlayAudioFromBytes(data, btnPlay);

            p.Controls.Add(lSender);
            p.Controls.Add(btnPlay);
            flowMessages.Controls.Add(p);
            flowMessages.ScrollControlIntoView(p);
        }

        // ---------------- Update user list ----------------
        private void UpdateUserList(List<string> users)
        {
            if (InvokeRequired) { Invoke(new Action<List<string>>(UpdateUserList), users); return; }
            lstUsers.Items.Clear();
            foreach (var u in users) lstUsers.Items.Add(u);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            Disconnect();
            try { activeOutput?.Stop(); activeOutput?.Dispose(); } catch { }
        }
    }

    public static class ChatProtocol
    {
        public const int PORT = 5000;
    }
}
