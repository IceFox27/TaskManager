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
using AccountWorking;

namespace TaskManagerApplication
{
    public partial class RegisterForm : Form
    {
        public RegisterForm()
        {
            InitializeComponent();

            userNameField.Text = "Введите имя";
            userNameField.ForeColor = Color.Gray;

            userSurnameField.Text = "Введите фамилию";
            userSurnameField.ForeColor = Color.Gray;
        }

        private void СloseButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
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

        private void UserNameField_Enter(object sender, EventArgs e)
        {
            if (userNameField.Text == "Введите имя")
            {
                userNameField.Text = "";
                userNameField.ForeColor = Color.Black;
            }
        }

        private void UserNameField_Leave(object sender, EventArgs e)
        {
            if (userNameField.Text == "")
            {
                userNameField.Text = "Введите имя";
                userNameField.ForeColor = Color.Gray;
            }
        }

        private void UserSurnameField_Enter(object sender, EventArgs e)
        {
            if (userSurnameField.Text == "Введите фамилию")
            {
                userSurnameField.Text = "";
                userSurnameField.ForeColor = Color.Black;
            }
        }

        private void UserSurnameField_Leave(object sender, EventArgs e)
        {
            if (userSurnameField.Text == "")
            {
                userSurnameField.Text = "Введите фамилию";
                userSurnameField.ForeColor = Color.Gray;
            }
        }

        private void ButtonRegister_Click(object sender, EventArgs e)
        {
            if (userNameField.Text == "Введите имя" || string.IsNullOrWhiteSpace(userNameField.Text))
            {
                MessageBox.Show("Введите имя", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                userNameField.Focus();
                return;
            }

            if (userSurnameField.Text == "Введите фамилию" || string.IsNullOrWhiteSpace(userSurnameField.Text))
            {
                MessageBox.Show("Введите фамилию", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                userSurnameField.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(loginField.Text))
            {
                MessageBox.Show("Введите логин", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                loginField.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(passwordField.Text))
            {
                MessageBox.Show("Введите пароль", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                passwordField.Focus();
                return;
            }

            AuthSystem authSystem = new AuthSystem();

            var existsCheck = authSystem.IsUserExists(loginField.Text);
            if (existsCheck.exists)
            {
                MessageBox.Show("Такой логин уже существует, введите другой", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                loginField.Focus();
                return;
            }

            var passwordStrength = authSystem.CheckPasswordStrength(passwordField.Text);
            if (passwordStrength.strength < 3)
            {
                var requirements = authSystem.GetPasswordRequirements();
                MessageBox.Show($"Слабый пароль!\n{passwordStrength.message}\n\n{requirements}",
                    "Небезопасный пароль",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var registrationResult = authSystem.Register(
                loginField.Text,
                passwordField.Text,
                userNameField.Text,
                userSurnameField.Text
            );

            if (registrationResult.success)
            {
                MessageBox.Show(registrationResult.message, "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.Hide();
                LoginForm loginForm = new LoginForm();
                loginForm.Show();
            }
            else
            {
                MessageBox.Show(registrationResult.message, "Ошибка регистрации", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowPasswordRequirements_Click(object sender, EventArgs e)
        {
            AuthSystem authSystem = new AuthSystem();
            string requirements = authSystem.GetPasswordRequirements();
            MessageBox.Show(requirements, "Требования к паролю", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RegisterLabel_Click(object sender, EventArgs e)
        {
            this.Hide();
            LoginForm loginForm = new LoginForm();
            loginForm.Show();
        }

        private void LoginField_TextChanged(object sender, EventArgs e)
        {
            UpdateRegisterButtonState();
        }

        private void PasswordField_TextChanged(object sender, EventArgs e)
        {
            UpdateRegisterButtonState();

            if (!string.IsNullOrWhiteSpace(passwordField.Text))
            {
                AuthSystem authSystem = new AuthSystem();
                var strength = authSystem.CheckPasswordStrength(passwordField.Text);

                switch (strength.strength)
                {
                    case 5:
                        passwordField.BorderStyle = BorderStyle.Fixed3D;
                        passwordField.BackColor = Color.LightGreen;
                        break;
                    case 4:
                        passwordField.BorderStyle = BorderStyle.Fixed3D;
                        passwordField.BackColor = Color.LightBlue;
                        break;
                    case 3:
                        passwordField.BorderStyle = BorderStyle.Fixed3D;
                        passwordField.BackColor = Color.LightYellow;
                        break;
                    case 2:
                        passwordField.BorderStyle = BorderStyle.Fixed3D;
                        passwordField.BackColor = Color.LightCoral;
                        break;
                    default:
                        passwordField.BorderStyle = BorderStyle.Fixed3D;
                        passwordField.BackColor = Color.White;
                        break;
                }
            }
            else
            {
                passwordField.BackColor = Color.White;
            }
        }

        private void UpdateRegisterButtonState()
        {
            bool hasName = !string.IsNullOrWhiteSpace(userNameField.Text) && userNameField.Text != "Введите имя";
            bool hasSurname = !string.IsNullOrWhiteSpace(userSurnameField.Text) && userSurnameField.Text != "Введите фамилию";
            bool hasLogin = !string.IsNullOrWhiteSpace(loginField.Text);
            bool hasPassword = !string.IsNullOrWhiteSpace(passwordField.Text);

            buttonRegister.Enabled = hasName && hasSurname && hasLogin && hasPassword;
        }

        private void CheckPasswordStrength_Click_1(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(passwordField.Text))
            {
                AuthSystem authSystem = new AuthSystem();
                var strength = authSystem.CheckPasswordStrength(passwordField.Text);
                MessageBox.Show(strength.message, "Проверка пароля", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Введите пароль для проверки", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}