namespace BTL_Video
{
    partial class VideoCallForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.PictureBox pictureBoxVideo;
        private System.Windows.Forms.PictureBox pictureBoxLocal;
        private System.Windows.Forms.Button btnToggleCamera;
        private System.Windows.Forms.Button btnToggleMic;
        private System.Windows.Forms.Label lblLocalPorts;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pictureBoxVideo = new System.Windows.Forms.PictureBox();
            this.pictureBoxLocal = new System.Windows.Forms.PictureBox();
            this.btnToggleCamera = new System.Windows.Forms.Button();
            this.btnToggleMic = new System.Windows.Forms.Button();
            this.lblLocalPorts = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxVideo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLocal)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBoxVideo (remote)
            // 
            this.pictureBoxVideo.Location = new System.Drawing.Point(330, 12);
            this.pictureBoxVideo.Size = new System.Drawing.Size(320, 240);
            this.pictureBoxVideo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            // 
            // pictureBoxLocal (your own)
            // 
            this.pictureBoxLocal.Location = new System.Drawing.Point(12, 12);
            this.pictureBoxLocal.Size = new System.Drawing.Size(320, 240);
            this.pictureBoxLocal.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            // 
            // btnToggleCamera
            // 
            this.btnToggleCamera.Location = new System.Drawing.Point(12, 260);
            this.btnToggleCamera.Size = new System.Drawing.Size(140, 30);
            this.btnToggleCamera.Text = "Turn Camera Off";
            this.btnToggleCamera.Click += new System.EventHandler(this.btnToggleCamera_Click);
            // 
            // btnToggleMic
            // 
            this.btnToggleMic.Location = new System.Drawing.Point(170, 260);
            this.btnToggleMic.Size = new System.Drawing.Size(140, 30);
            this.btnToggleMic.Text = "Mute Microphone";
            this.btnToggleMic.Click += new System.EventHandler(this.btnToggleMic_Click);
            // 
            // lblLocalPorts
            // 
            this.lblLocalPorts.Location = new System.Drawing.Point(330, 260);
            this.lblLocalPorts.Size = new System.Drawing.Size(320, 30);
            this.lblLocalPorts.Text = "Local ports: video=? audio=?";
            // 
            // VideoCallForm
            // 
            this.ClientSize = new System.Drawing.Size(664, 304);
            this.Controls.Add(this.lblLocalPorts);
            this.Controls.Add(this.btnToggleMic);
            this.Controls.Add(this.btnToggleCamera);
            this.Controls.Add(this.pictureBoxVideo);
            this.Controls.Add(this.pictureBoxLocal);
            this.Name = "VideoCallForm";
            this.Text = "Video Call";
            this.Load += new System.EventHandler(this.VideoCallForm_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.VideoCallForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxVideo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLocal)).EndInit();
            this.ResumeLayout(false);
        }
    }
}