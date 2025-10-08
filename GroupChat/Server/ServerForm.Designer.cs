namespace ChatServerApp
{
    partial class ServerForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            txtLog = new TextBox();
            listUser = new ListBox();
            btnStart = new Button();
            lblDanhSach = new Label();
            label1 = new Label();
            SuspendLayout();
            // 
            // txtLog
            // 
            txtLog.BackColor = SystemColors.ControlLightLight;
            txtLog.Location = new Point(103, 62);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(571, 219);
            txtLog.TabIndex = 0;
            // 
            // listUser
            // 
            listUser.FormattingEnabled = true;
            listUser.Location = new Point(103, 333);
            listUser.Name = "listUser";
            listUser.Size = new Size(437, 104);
            listUser.TabIndex = 1;
            // 
            // btnStart
            // 
            btnStart.Location = new Point(580, 332);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(94, 105);
            btnStart.TabIndex = 2;
            btnStart.Text = "Start Server";
            btnStart.UseVisualStyleBackColor = true;
            // 
            // lblDanhSach
            // 
            lblDanhSach.AutoSize = true;
            lblDanhSach.Font = new Font("Segoe UI Semibold", 10.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblDanhSach.Location = new Point(103, 295);
            lblDanhSach.Name = "lblDanhSach";
            lblDanhSach.Size = new Size(226, 25);
            lblDanhSach.TabIndex = 3;
            lblDanhSach.Text = "Danh sách người tham gia";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(243, 19);
            label1.Name = "label1";
            label1.Size = new Size(297, 28);
            label1.TabIndex = 4;
            label1.Text = "CHAT SERVER CONTROL PANEL";
            // 
            // ServerForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(791, 480);
            Controls.Add(label1);
            Controls.Add(lblDanhSach);
            Controls.Add(btnStart);
            Controls.Add(listUser);
            Controls.Add(txtLog);
            Name = "ServerForm";
            Text = "Chat Server";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtLog;
        private ListBox listUser;
        private Button btnStart;
        private Label lblDanhSach;
        private Label label1;
    }
}
