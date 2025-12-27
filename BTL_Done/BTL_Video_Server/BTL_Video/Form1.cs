using System;
using System.Windows.Forms;

namespace BTL_Video
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnRunServer_Click(object sender, EventArgs e)
        {
            var serverForm = new ServerForm();
            serverForm.Show();
            Hide();
        }

        private void btnRunClient_Click(object sender, EventArgs e)
        {
            var login = new LoginForm();
            login.Show();
            Hide();
        }
    }
}
