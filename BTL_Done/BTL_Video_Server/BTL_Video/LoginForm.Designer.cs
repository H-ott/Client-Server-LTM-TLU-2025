namespace BTL_Video
{
    partial class LoginForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox txtUser;
        private System.Windows.Forms.TextBox txtPass;
        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.Button btnRegister;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.TextBox txtServer;
        private System.Windows.Forms.Label lblServer;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtUser = new System.Windows.Forms.TextBox();
            this.txtPass = new System.Windows.Forms.TextBox();
            this.btnLogin = new System.Windows.Forms.Button();
            this.btnRegister = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.txtServer = new System.Windows.Forms.TextBox();
            this.lblServer = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // txtServer
            // 
            this.txtServer.Location = new System.Drawing.Point(90, 12);
            this.txtServer.Size = new System.Drawing.Size(180, 23);
            this.txtServer.Text = "127.0.0.1";
            // 
            // lblServer
            // 
            this.lblServer.Location = new System.Drawing.Point(12, 12);
            this.lblServer.Size = new System.Drawing.Size(72, 23);
            this.lblServer.Text = "Server IP:";
            // 
            // txtUser
            // 
            this.txtUser.Location = new System.Drawing.Point(90, 50);
            this.txtUser.Size = new System.Drawing.Size(180, 23);
            this.txtUser.PlaceholderText = "Username";
            // 
            // txtPass
            // 
            this.txtPass.Location = new System.Drawing.Point(90, 86);
            this.txtPass.Size = new System.Drawing.Size(180, 23);
            this.txtPass.PlaceholderText = "Password";
            this.txtPass.UseSystemPasswordChar = true;
            // 
            // btnLogin
            // 
            this.btnLogin.Location = new System.Drawing.Point(90, 120);
            this.btnLogin.Size = new System.Drawing.Size(100, 30);
            this.btnLogin.Text = "Login";
            this.btnLogin.Click += new System.EventHandler(this.btnLogin_Click);
            // 
            // btnRegister
            // 
            this.btnRegister.Location = new System.Drawing.Point(200, 120);
            this.btnRegister.Size = new System.Drawing.Size(100, 30);
            this.btnRegister.Text = "Register";
            this.btnRegister.Click += new System.EventHandler(this.btnRegister_Click);
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(12, 170);
            this.txtLog.Multiline = true;
            this.txtLog.Size = new System.Drawing.Size(380, 150);
            this.txtLog.ReadOnly = true;
            // 
            // LoginForm
            // 
            this.ClientSize = new System.Drawing.Size(404, 332);
            this.Controls.Add(this.lblServer);
            this.Controls.Add(this.txtServer);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.btnRegister);
            this.Controls.Add(this.btnLogin);
            this.Controls.Add(this.txtPass);
            this.Controls.Add(this.txtUser);
            this.Name = "LoginForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "BTL Video - Login";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}