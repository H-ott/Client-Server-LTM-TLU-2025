// ServerForm.Designer.cs
using System.Windows.Forms;
namespace ChatServerApp
{
    partial class ChatServerApp :Form
    {
        private System.ComponentModel.IContainer components = null;
        private Button btnStart;
        private Button btnStop;
        private Label lblPort;
        private TextBox txtPort;
        private ListBox lstLog;
        private ListBox lstClients;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            btnStart = new Button();
            btnStop = new Button();
            lblPort = new Label();
            txtPort = new TextBox();
            lstLog = new ListBox();
            lstClients = new ListBox();
            SuspendLayout();
            // 
            // btnStart
            // 
            btnStart.Location = new Point(140, 8);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(75, 27);
            btnStart.TabIndex = 2;
            btnStart.Text = "Start";
            btnStart.Click += BtnStart_Click;
            // 
            // btnStop
            // 
            btnStop.Location = new Point(220, 8);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(75, 27);
            btnStop.TabIndex = 3;
            btnStop.Text = "Stop";
            btnStop.Click += BtnStop_Click;
            // 
            // lblPort
            // 
            lblPort.Location = new Point(10, 8);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(35, 23);
            lblPort.TabIndex = 0;
            lblPort.Text = "Port";
            // 
            // txtPort
            // 
            txtPort.Location = new Point(51, 8);
            txtPort.Name = "txtPort";
            txtPort.Size = new Size(80, 27);
            txtPort.TabIndex = 1;
            txtPort.Text = "5000";
            // 
            // lstLog
            // 
            lstLog.Location = new Point(220, 60);
            lstLog.Name = "lstLog";
            lstLog.Size = new Size(560, 404);
            lstLog.TabIndex = 5;
            // 
            // lstClients
            // 
            lstClients.Location = new Point(10, 40);
            lstClients.Name = "lstClients";
            lstClients.Size = new Size(200, 424);
            lstClients.TabIndex = 4;
            // 
            // ChatServerApp
            // 
            ClientSize = new Size(800, 500);
            Controls.Add(lblPort);
            Controls.Add(txtPort);
            Controls.Add(btnStart);
            Controls.Add(btnStop);
            Controls.Add(lstClients);
            Controls.Add(lstLog);
            Name = "ChatServerApp";
            Text = "Chat Server";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
