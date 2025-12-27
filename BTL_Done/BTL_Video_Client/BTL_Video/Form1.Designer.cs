namespace BTL_Video
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
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
            btnRunClient = new Button();
            SuspendLayout();
            // 
            // btnRunClient
            // 
            btnRunClient.Location = new Point(28, 52);
            btnRunClient.Name = "btnRunClient";
            btnRunClient.Size = new Size(220, 40);
            btnRunClient.TabIndex = 1;
            btnRunClient.Text = "Run as Client";
            btnRunClient.UseVisualStyleBackColor = true;
            btnRunClient.Click += btnRunClient_Click;
            // 
            // Form1
            // 
            ClientSize = new Size(284, 151);
            Controls.Add(btnRunClient);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "BTL Video - Start";
            ResumeLayout(false);
        }

        #endregion
    }
}
