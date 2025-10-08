using System.Windows.Forms;

namespace ChatClientApp
{
    partial class ClientForm
    {
        private System.ComponentModel.IContainer components = null;

        private TextBox txtName;
        private TextBox txtServerIP;
        private Button btnConnect;
        private ListBox listChat;
        private TextBox txtMessage;
        private Button btnSend;
        private Label lblStatus; 

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            txtName = new TextBox();
            txtServerIP = new TextBox();
            btnConnect = new Button();
            listChat = new ListBox();
            txtMessage = new TextBox();
            btnSend = new Button();
            lblStatus = new Label();
            lblname = new Label();
            lblIP = new Label();
            ListUsers = new ListBox();
            SuspendLayout();
            // 
            // txtName
            // 
            txtName.Location = new Point(214, 18);
            txtName.Margin = new Padding(3, 4, 3, 4);
            txtName.Name = "txtName";
            txtName.Size = new Size(241, 27);
            txtName.TabIndex = 0;
            // 
            // txtServerIP
            // 
            txtServerIP.Location = new Point(214, 56);
            txtServerIP.Margin = new Padding(3, 4, 3, 4);
            txtServerIP.Name = "txtServerIP";
            txtServerIP.Size = new Size(241, 27);
            txtServerIP.TabIndex = 1;
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(506, 37);
            btnConnect.Margin = new Padding(3, 4, 3, 4);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(100, 31);
            btnConnect.TabIndex = 2;
            btnConnect.Text = "Kết nối";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // listChat
            // 
            listChat.FormattingEnabled = true;
            listChat.Location = new Point(27, 105);
            listChat.Margin = new Padding(3, 4, 3, 4);
            listChat.Name = "listChat";
            listChat.Size = new Size(318, 264);
            listChat.TabIndex = 3;
            // 
            // txtMessage
            // 
            txtMessage.Location = new Point(27, 383);
            txtMessage.Margin = new Padding(3, 4, 3, 4);
            txtMessage.Name = "txtMessage";
            txtMessage.Size = new Size(468, 27);
            txtMessage.TabIndex = 4;
            // 
            // btnSend
            // 
            btnSend.Location = new Point(520, 383);
            btnSend.Margin = new Padding(3, 4, 3, 4);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(86, 31);
            btnSend.TabIndex = 5;
            btnSend.Text = "Gửi";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = SystemColors.ActiveCaptionText;
            lblStatus.Location = new Point(27, 420);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(160, 20);
            lblStatus.TabIndex = 6;
            lblStatus.Text = "🔴 Chưa kết nối server";
            // 
            // lblname
            // 
            lblname.AutoSize = true;
            lblname.Location = new Point(67, 25);
            lblname.Name = "lblname";
            lblname.Size = new Size(75, 20);
            lblname.TabIndex = 7;
            lblname.Text = "Username";
            // 
            // lblIP
            // 
            lblIP.AutoSize = true;
            lblIP.Location = new Point(67, 63);
            lblIP.Name = "lblIP";
            lblIP.Size = new Size(123, 20);
            lblIP.TabIndex = 8;
            lblIP.Text = "Server IP Address";
            // 
            // ListUsers
            // 
            ListUsers.FormattingEnabled = true;
            ListUsers.Location = new Point(404, 105);
            ListUsers.Name = "ListUsers";
            ListUsers.Size = new Size(202, 264);
            ListUsers.TabIndex = 10;
            // 
            // ClientForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(649, 462);
            Controls.Add(ListUsers);
            Controls.Add(lblIP);
            Controls.Add(lblname);
            Controls.Add(lblStatus);
            Controls.Add(btnSend);
            Controls.Add(txtMessage);
            Controls.Add(listChat);
            Controls.Add(btnConnect);
            Controls.Add(txtServerIP);
            Controls.Add(txtName);
            Margin = new Padding(3, 4, 3, 4);
            Name = "ClientForm";
            Text = "Chat Client";
            ResumeLayout(false);
            PerformLayout();
        }
        private Label lblname;
        private Label lblIP;
        private ListBox ListUsers;
    }
}
