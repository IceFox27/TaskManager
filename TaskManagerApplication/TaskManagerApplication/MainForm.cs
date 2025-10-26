using System;
using System.Windows.Forms;

namespace TaskManagerApplication
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }
        
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
           Application.Exit();
        }
    }
}