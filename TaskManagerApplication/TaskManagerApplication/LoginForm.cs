using AccountWorking;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaskManagerApplication
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();

            this.passwordField.AutoSize = false;
            this.passwordField.Size = new Size(this.passwordField.Size.Width, 64);
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        
        private void CloseButton_MouseEnter(object sender, EventArgs e)
        {
            closeButton.ForeColor = Color.Green;
        }

        private void CloseButton_MouseLeave(object sender, EventArgs e)
        {
            closeButton.ForeColor = Color.White;
        }

        Point lastPoint;

        private void MainPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Left += e.X - lastPoint.X;
                this.Top += e.Y - lastPoint.Y;
            }
        }

        private void MainPanel_MouseDown(object sender, MouseEventArgs e)
        {
            lastPoint = new Point(e.X, e.Y);
        }

        private void ButtonLogin_Click(object sender, EventArgs e)
        {
            string loginUser = loginField.Text;
            string passwordUser = passwordField.Text;

            if (string.IsNullOrWhiteSpace(loginUser))
            {
                MessageBox.Show("Введите логин", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                loginField.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(passwordUser))
            {
                MessageBox.Show("Введите пароль", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                passwordField.Focus();
                return;
            }

            AuthSystem authSystem = new AuthSystem();

            var loginResult = authSystem.Login(loginUser, passwordUser);

            if (loginResult.success)
            {
                MessageBox.Show(loginResult.message, "Успешный вход", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.Hide();
                MainForm mainForm = new MainForm();
                mainForm.Show();
            }
            else
            {
                MessageBox.Show(loginResult.message, "Ошибка входа", MessageBoxButtons.OK, MessageBoxIcon.Error);

                passwordField.Text = "";
                passwordField.Focus();
            }
        }

        private void RegisterLabel_Click(object sender, EventArgs e)
        {
            this.Hide();
            RegisterForm registerForm = new RegisterForm();
            registerForm.Show();
        }
    }
}