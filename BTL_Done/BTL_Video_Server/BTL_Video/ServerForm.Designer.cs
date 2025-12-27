namespace BTL_Video
{
    partial class ServerForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.ListBox lstOnline;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.lstOnline = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(12, 12);
            this.btnStart.Size = new System.Drawing.Size(120, 28);
            this.btnStart.Text = "Start Server";
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(150, 12);
            this.btnStop.Size = new System.Drawing.Size(120, 28);
            this.btnStop.Text = "Stop Server";
            this.btnStop.Enabled = false;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(12, 52);
            this.txtLog.Multiline = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(520, 300);
            // 
            // lstOnline
            // 
            this.lstOnline.Location = new System.Drawing.Point(540, 52);
            this.lstOnline.Size = new System.Drawing.Size(150, 300);
            // 
            // ServerForm
            // 
            this.ClientSize = new System.Drawing.Size(706, 370);
            this.Controls.Add(this.lstOnline);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnStart);
            this.Name = "ServerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "BTL Video - Server";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}