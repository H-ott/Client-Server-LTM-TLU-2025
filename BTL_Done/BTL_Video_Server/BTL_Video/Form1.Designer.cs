namespace BTL_Video
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnRunServer;
        private System.Windows.Forms.Button btnRunClient;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnRunServer = new System.Windows.Forms.Button();
            this.btnRunClient = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnRunServer
            // 
            this.btnRunServer.Location = new System.Drawing.Point(30, 25);
            this.btnRunServer.Name = "btnRunServer";
            this.btnRunServer.Size = new System.Drawing.Size(220, 40);
            this.btnRunServer.TabIndex = 0;
            this.btnRunServer.Text = "Run as Server (this machine)";
            this.btnRunServer.UseVisualStyleBackColor = true;
            this.btnRunServer.Click += new System.EventHandler(this.btnRunServer_Click);
            // 
            // btnRunClient
            // 
            this.btnRunClient.Location = new System.Drawing.Point(30, 80);
            this.btnRunClient.Name = "btnRunClient";
            this.btnRunClient.Size = new System.Drawing.Size(220, 40);
            this.btnRunClient.TabIndex = 1;
            this.btnRunClient.Text = "Run as Client";
            this.btnRunClient.UseVisualStyleBackColor = true;
            this.btnRunClient.Click += new System.EventHandler(this.btnRunClient_Click);
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(284, 151);
            this.Controls.Add(this.btnRunClient);
            this.Controls.Add(this.btnRunServer);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "BTL Video - Start";
            this.ResumeLayout(false);
        }

        #endregion
    }
}
