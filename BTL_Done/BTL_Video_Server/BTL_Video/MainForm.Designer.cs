namespace BTL_Video
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListBox lstOnline;
        private System.Windows.Forms.TextBox txtChat;
        private System.Windows.Forms.TextBox txtMsg;
        private System.Windows.Forms.Button btnSendMsg;
        private System.Windows.Forms.Button btnSendFile;
        private System.Windows.Forms.Button btnCall;
        private System.Windows.Forms.TextBox txtLog;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lstOnline = new System.Windows.Forms.ListBox();
            this.txtChat = new System.Windows.Forms.TextBox();
            this.txtMsg = new System.Windows.Forms.TextBox();
            this.btnSendMsg = new System.Windows.Forms.Button();
            this.btnSendFile = new System.Windows.Forms.Button();
            this.btnCall = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // lstOnline
            // 
            this.lstOnline.Location = new System.Drawing.Point(12, 12);
            this.lstOnline.Size = new System.Drawing.Size(160, 300);
            // 
            // txtChat
            // 
            this.txtChat.Location = new System.Drawing.Point(190, 12);
            this.txtChat.Multiline = true;
            this.txtChat.Size = new System.Drawing.Size(420, 220);
            this.txtChat.ReadOnly = true;
            // 
            // txtMsg
            // 
            this.txtMsg.Location = new System.Drawing.Point(190, 240);
            this.txtMsg.Size = new System.Drawing.Size(330, 23);
            // 
            // btnSendMsg
            // 
            this.btnSendMsg.Location = new System.Drawing.Point(530, 238);
            this.btnSendMsg.Size = new System.Drawing.Size(80, 26);
            this.btnSendMsg.Text = "Send";
            this.btnSendMsg.Click += new System.EventHandler(this.btnSendMsg_Click);
            // 
            // btnSendFile
            // 
            this.btnSendFile.Location = new System.Drawing.Point(190, 270);
            this.btnSendFile.Size = new System.Drawing.Size(120, 26);
            this.btnSendFile.Text = "Send File";
            this.btnSendFile.Click += new System.EventHandler(this.btnSendFile_Click);
            // 
            // btnCall
            // 
            this.btnCall.Location = new System.Drawing.Point(320, 270);
            this.btnCall.Size = new System.Drawing.Size(120, 26);
            this.btnCall.Text = "Call Video";
            this.btnCall.Click += new System.EventHandler(this.btnCall_Click);
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(190, 310);
            this.txtLog.Multiline = true;
            this.txtLog.Size = new System.Drawing.Size(420, 110);
            this.txtLog.ReadOnly = true;
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(624, 431);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.btnCall);
            this.Controls.Add(this.btnSendFile);
            this.Controls.Add(this.btnSendMsg);
            this.Controls.Add(this.txtMsg);
            this.Controls.Add(this.txtChat);
            this.Controls.Add(this.lstOnline);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "BTL Video - Main";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}