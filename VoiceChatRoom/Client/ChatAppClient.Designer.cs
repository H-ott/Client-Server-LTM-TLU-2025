using System;
using System.Windows.Forms;

namespace ChatAppClient
{
    partial class ClientForm : Form
    {
        private System.ComponentModel.IContainer components = null;
        private Panel panelLeft;
        private Panel panelCenter;
        private Panel panelRight;

        private Label lblChats, lblUser, lblIp, lblActive, lblRoom, lblLog;
        private TextBox txtUsername, txtServerIp, txtMessage, txtLog;
        private Button btnConnect, btnSend, btnSendImage, btnSendFile, btnStartVoice, btnStopVoice;
        private ListBox lstUsers;
        private FlowLayoutPanel flowMessages;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            this.SuspendLayout();

            // === FORM ===
            this.ClientSize = new System.Drawing.Size(1280, 720);
            this.Text = "Chat Client";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = System.Drawing.Color.LightGray;

            // === PANEL TRÁI ===
            panelLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 260,
                BackColor = System.Drawing.Color.WhiteSmoke
            };

            lblChats = new Label
            {
                Text = "CHATS",
                Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Top = 12,
                Left = 16
            };
            panelLeft.Controls.Add(lblChats);

            lblUser = new Label
            {
                Text = "Tên người dùng:",
                Left = 12,
                Top = 52,
                AutoSize = true
            };
            panelLeft.Controls.Add(lblUser);

            txtUsername = new TextBox
            {
                Left = 12,
                Top = 72,
                Width = 220,
                Text = "User"
            };
            panelLeft.Controls.Add(txtUsername);

            lblIp = new Label
            {
                Text = "Địa chỉ Server:",
                Left = 12,
                Top = 104,
                AutoSize = true
            };
            panelLeft.Controls.Add(lblIp);

            txtServerIp = new TextBox
            {
                Left = 12,
                Top = 124,
                Width = 160,
                Text = "127.0.0.1"
            };
            panelLeft.Controls.Add(txtServerIp);

            btnConnect = new Button
            {
                Left = 180,
                Top = 122,
                Width = 60,
                Text = "Kết nối"
            };
            btnConnect.Click += new EventHandler(this.btnConnect_Click);
            panelLeft.Controls.Add(btnConnect);

            lblActive = new Label
            {
                Text = "Người đang hoạt động:",
                Left = 12,
                Top = 160,
                AutoSize = true
            };
            panelLeft.Controls.Add(lblActive);

            lstUsers = new ListBox
            {
                Left = 12,
                Top = 184,
                Width = 228,
                Height = 460
            };
            panelLeft.Controls.Add(lstUsers);

            // === PANEL PHẢI ===
            panelRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = 250,
                BackColor = System.Drawing.Color.Gainsboro
            };

            lblLog = new Label
            {
                Text = "Ghi chú / Nhật ký",
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Top = 12,
                Left = 16
            };
            panelRight.Controls.Add(lblLog);

            txtLog = new TextBox
            {
                Left = 12,
                Top = 40,
                Width = 220,
                Height = 640,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };
            panelRight.Controls.Add(txtLog);

            // === PANEL GIỮA ===
            panelCenter = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.White,
                Padding = new Padding(20, 10, 20, 10)
            };

            lblRoom = new Label
            {
                Text = "Phòng: General",
                Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Top = 10,
                Left = 20
            };
            panelCenter.Controls.Add(lblRoom);

            flowMessages = new FlowLayoutPanel
            {
                Left = 20,
                Top = 45,
                Width = 700,
                Height = 540,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = System.Drawing.Color.FromArgb(245, 245, 245)
            };
            panelCenter.Controls.Add(flowMessages);

            txtMessage = new TextBox
            {
                Left = 20,
                Top = 600,
                Width = 400,
                Height = 30
            };
            panelCenter.Controls.Add(txtMessage);

            btnSend = new Button
            {
                Left = 430,
                Top = 600,
                Width = 60,
                Text = "Gửi"
            };
            btnSend.Click += new EventHandler(this.btnSend_Click);
            panelCenter.Controls.Add(btnSend);

            btnSendImage = new Button
            {
                Left = 500,
                Top = 600,
                Width = 60,
                Text = "Ảnh"
            };
            btnSendImage.Click += new EventHandler(this.btnSendImage_Click);
            panelCenter.Controls.Add(btnSendImage);

            btnSendFile = new Button
            {
                Left = 570,
                Top = 600,
                Width = 60,
                Text = "File"
            };
            btnSendFile.Click += new EventHandler(this.btnSendFile_Click);
            panelCenter.Controls.Add(btnSendFile);

            btnStartVoice = new Button
            {
                Left = 640,
                Top = 600,
                Width = 60,
                Text = "Voice"
            };
            btnStartVoice.Click += new EventHandler(this.btnStartVoice_Click);
            panelCenter.Controls.Add(btnStartVoice);

            btnStopVoice = new Button
            {
                Left = 710,
                Top = 600,
                Width = 60,
                Text = "Dừng",
                Enabled = false
            };
            btnStopVoice.Click += new EventHandler(this.btnStopVoice_Click);
            panelCenter.Controls.Add(btnStopVoice);

            // === ADD ALL ===
            this.Controls.Add(panelCenter);
            this.Controls.Add(panelRight);
            this.Controls.Add(panelLeft);

            this.ResumeLayout(false);
        }
    }
}
