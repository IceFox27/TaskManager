using System.Data;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;

namespace AccountWorking
{
    public class AuthSystem
    {
        private Database _database;

        public AuthSystem()
        {
            _database = new Database();
        }

        private const int MIN_PASSWORD_LENGTH = 8;
        private const int MAX_PASSWORD_LENGTH = 64;
        private const int MIN_LOGIN_LENGTH = 3;
        private const int MAX_LOGIN_LENGTH = 50;

        private string GenerateSalt()
        {
            try
            {
                byte[] saltBytes = new byte[16];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(saltBytes);
                }
                return Convert.ToBase64String(saltBytes);
            }
            catch (CryptographicException ex)
            {
                throw new Exception("Ошибка генерации соли: " + ex.Message);
            }
        }

        private string HashPassword(string password, string salt)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Пароль не может быть пустым");

            if (string.IsNullOrEmpty(salt))
                throw new ArgumentException("Соль не может быть пустой");

            try
            {
                using (var sha256 = SHA256.Create())
                {
                    byte[] passwordBytes = Encoding.UTF8.GetBytes(password + salt);
                    byte[] hashBytes = sha256.ComputeHash(passwordBytes);
                    return Convert.ToBase64String(hashBytes);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка хеширования пароля: " + ex.Message);
            }
        }

        private bool VerifyPassword(string password, string salt, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(salt) || string.IsNullOrEmpty(storedHash))
                return false;

            try
            {
                string computedHash = HashPassword(password, salt);
                return computedHash == storedHash;
            }
            catch
            {
                return false;
            }
        }

        private bool ValidateLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
                return false;

            if (login.Length < MIN_LOGIN_LENGTH || login.Length > MAX_LOGIN_LENGTH)
                return false;

            return Regex.IsMatch(login, @"^[a-zA-Z0-9_]+$");
        }

        private bool ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            if (password.Length < MIN_PASSWORD_LENGTH || password.Length > MAX_PASSWORD_LENGTH)
                return false;

            bool hasUpperCase = Regex.IsMatch(password, "[A-Z]");
            bool hasLowerCase = Regex.IsMatch(password, "[a-z]");
            bool hasDigits = Regex.IsMatch(password, "[0-9]");
            bool hasSpecialChars = Regex.IsMatch(password, "[!@#$%^&*()_+\\-=\\[\\]{};':\"\\\\|,.<>\\/?]");

            return hasUpperCase && hasLowerCase && hasDigits && hasSpecialChars;
        }

        private bool ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (name.Length < 2 || name.Length > 50)
                return false;

            return Regex.IsMatch(name, @"^[a-zA-Zа-яА-Я\-]+$");
        }

        public (bool success, string message) Login(string login, string password)
        {
            try
            {
                if (!ValidateLogin(login))
                    return (false, "Неверный формат логина");

                if (string.IsNullOrEmpty(password))
                    return (false, "Пароль не может быть пустым");

                DataTable table = new DataTable();
                MySqlDataAdapter adapter = new MySqlDataAdapter();

                using (MySqlCommand command = new MySqlCommand(
                    "SELECT * FROM `users` WHERE `login` = @uL",
                    _database.GetConnection()))
                {
                    command.Parameters.Add("@uL", MySqlDbType.VarChar).Value = login;

                    adapter.SelectCommand = command;
                    adapter.Fill(table);
                }

                if (table.Rows.Count > 0)
                {
                    DataRow user = table.Rows[0];
                    string storedSalt = user["salt"]?.ToString() ?? "";
                    string storedPasswordHash = user["password"]?.ToString() ?? "";

                    bool passwordValid = VerifyPassword(password, storedSalt, storedPasswordHash);

                    if (passwordValid)
                    {
                        return (true, "Успешный вход");
                    }
                    else
                    {
                        return (false, "Неверный пароль");
                    }
                }

                return (false, "Пользователь не найден");
            }
            catch (MySqlException ex)
            {
                return (false, $"Ошибка базы данных: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка системы: {ex.Message}");
            }
        }

        public (bool success, string message) Register(string login, string password, string name, string surname)
        {
            try
            {
                if (!ValidateLogin(login))
                    return (false, $"Логин должен содержать от {MIN_LOGIN_LENGTH} до {MAX_LOGIN_LENGTH} символов (только буквы, цифры и подчеркивания)");

                if (!ValidatePassword(password))
                    return (false, $"Пароль должен содержать от {MIN_PASSWORD_LENGTH} до {MAX_PASSWORD_LENGTH} символов, включая заглавные и строчные буквы, цифры и специальные символы");

                if (!ValidateName(name))
                    return (false, "Имя должно содержать от 2 до 50 символов (только буквы и дефисы)");

                if (!ValidateName(surname))
                    return (false, "Фамилия должна содержать от 2 до 50 символов (только буквы и дефисы)");

                var existsCheck = IsUserExists(login);
                if (existsCheck.exists)
                    return (false, "Пользователь с таким логином уже существует");

                string salt = GenerateSalt();
                string passwordHash = HashPassword(password, salt);

                using (MySqlCommand command = new MySqlCommand(
                    "INSERT INTO `users` (`login`, `password`, `name`, `surname`, `salt`) " +
                    "VALUES (@login, @pass, @name, @surname, @salt)",
                    _database.GetConnection()))
                {
                    command.Parameters.Add("@login", MySqlDbType.VarChar).Value = login;
                    command.Parameters.Add("@pass", MySqlDbType.VarChar).Value = passwordHash;
                    command.Parameters.Add("@name", MySqlDbType.VarChar).Value = name;
                    command.Parameters.Add("@surname", MySqlDbType.VarChar).Value = surname;
                    command.Parameters.Add("@salt", MySqlDbType.VarChar).Value = salt;

                    _database.OpenConnection();

                    try
                    {
                        int result = command.ExecuteNonQuery();
                        if (result == 1)
                            return (true, "Пользователь успешно зарегистрирован");
                        else
                            return (false, "Ошибка при создании пользователя");
                    }
                    catch (MySqlException ex) when (ex.Number == 1062) 
                    {
                        return (false, "Пользователь с таким логином уже существует");
                    }
                    catch (MySqlException ex)
                    {
                        return (false, $"Ошибка базы данных: {ex.Message}");
                    }
                    finally
                    {
                        _database.CloseConnection();
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка регистрации: {ex.Message}");
            }
        }

        public (bool exists, string message) IsUserExists(string login)
        {
            try
            {
                if (!ValidateLogin(login))
                    return (false, "Неверный формат логина");

                DataTable table = new DataTable();
                MySqlDataAdapter adapter = new MySqlDataAdapter();

                using (MySqlCommand command = new MySqlCommand(
                    "SELECT COUNT(*) FROM `users` WHERE `login` = @uL",
                    _database.GetConnection()))
                {
                    command.Parameters.Add("@uL", MySqlDbType.VarChar).Value = login;
                    adapter.SelectCommand = command;
                    adapter.Fill(table);
                }

                bool exists = table.Rows.Count > 0 && Convert.ToInt32(table.Rows[0][0]) > 0;
                return (exists, exists ? "Пользователь существует" : "Пользователь не существует");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка проверки: {ex.Message}");
            }
        }

        public (DataRow user, string message) GetUserInfo(string login)
        {
            try
            {
                if (!ValidateLogin(login))
                    return (null, "Неверный формат логина");

                DataTable table = new DataTable();
                MySqlDataAdapter adapter = new MySqlDataAdapter();

                using (MySqlCommand command = new MySqlCommand(
                    "SELECT * FROM `users` WHERE `login` = @uL",
                    _database.GetConnection()))
                {
                    command.Parameters.Add("@uL", MySqlDbType.VarChar).Value = login;
                    adapter.SelectCommand = command;
                    adapter.Fill(table);
                }

                if (table.Rows.Count > 0)
                {
                    DataRow user = table.Rows[0];
                    if (user.Table.Columns.Contains("password"))
                        user["password"] = "***";
                    if (user.Table.Columns.Contains("salt"))
                        user["salt"] = "***";

                    return (user, "Данные пользователя получены");
                }

                return (null, "Пользователь не найден");
            }
            catch (Exception ex)
            {
                return (null, $"Ошибка получения данных: {ex.Message}");
            }
        }

        public (bool success, string message) UpdatePassword(string login, string newPassword)
        {
            try
            {
                if (!ValidateLogin(login))
                    return (false, "Неверный формат логина");

                if (!ValidatePassword(newPassword))
                    return (false, $"Новый пароль не соответствует требованиям безопасности");

                var existsCheck = IsUserExists(login);
                if (!existsCheck.exists)
                    return (false, "Пользователь не существует");

                string salt = GenerateSalt();
                string passwordHash = HashPassword(newPassword, salt);

                using (MySqlCommand command = new MySqlCommand(
                    "UPDATE `users` SET `password` = @pass, `salt` = @salt WHERE `login` = @login",
                    _database.GetConnection()))
                {
                    command.Parameters.Add("@login", MySqlDbType.VarChar).Value = login;
                    command.Parameters.Add("@pass", MySqlDbType.VarChar).Value = passwordHash;
                    command.Parameters.Add("@salt", MySqlDbType.VarChar).Value = salt;

                    _database.OpenConnection();
                    int result = command.ExecuteNonQuery();
                    _database.CloseConnection();

                    if (result == 1)
                        return (true, "Пароль успешно обновлен");
                    else
                        return (false, "Не удалось обновить пароль");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка обновления пароля: {ex.Message}");
            }
        }

        public (bool success, string message) DeleteUser(string login)
        {
            try
            {
                if (!ValidateLogin(login))
                    return (false, "Неверный формат логина");

                var existsCheck = IsUserExists(login);
                if (!existsCheck.exists)
                    return (false, "Пользователь не существует");

                using (MySqlCommand command = new MySqlCommand(
                    "DELETE FROM `users` WHERE `login` = @login",
                    _database.GetConnection()))
                {
                    command.Parameters.Add("@login", MySqlDbType.VarChar).Value = login;

                    _database.OpenConnection();

                    try
                    {
                        int result = command.ExecuteNonQuery();
                        if (result == 1)
                            return (true, "Пользователь успешно удален");
                        else
                            return (false, "Не удалось удалить пользователя");
                    }
                    catch (MySqlException ex) when (ex.Number == 1451) 
                    {
                        return (false, "Невозможно удалить пользователя: имеются связанные данные");
                    }
                    catch (MySqlException ex)
                    {
                        return (false, $"Ошибка базы данных: {ex.Message}");
                    }
                    finally
                    {
                        _database.CloseConnection();
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка удаления: {ex.Message}");
            }
        }

        public string GetPasswordRequirements()
        {
            return $"Требования к паролю:\n" +
                   $"• Длина: от {MIN_PASSWORD_LENGTH} до {MAX_PASSWORD_LENGTH} символов\n" +
                   $"• Минимум одна заглавная буква\n" +
                   $"• Минимум одна строчная буква\n" +
                   $"• Минимум одна цифра\n" +
                   $"• Минимум один специальный символ (!@#$%^&*()_+-=[]{{}};':\"|,.<>/?)\n" +
                   $"• Без пробелов";
        }

        public (int strength, string message) CheckPasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return (0, "Пустой пароль");

            int strength = 0;
            string message = "";

            if (password.Length >= MIN_PASSWORD_LENGTH) strength++;
            if (Regex.IsMatch(password, "[A-Z]")) strength++;
            if (Regex.IsMatch(password, "[a-z]")) strength++;
            if (Regex.IsMatch(password, "[0-9]")) strength++;
            if (Regex.IsMatch(password, "[!@#$%^&*()_+\\-=\\[\\]{};':\"\\\\|,.<>\\/?]")) strength++;

            switch (strength)
            {
                case 5: message = "Очень сильный пароль"; break;
                case 4: message = "Сильный пароль"; break;
                case 3: message = "Средний пароль"; break;
                case 2: message = "Слабый пароль"; break;
                default: message = "Очень слабый пароль"; break;
            }

            return (strength, message);
        }
    }
}