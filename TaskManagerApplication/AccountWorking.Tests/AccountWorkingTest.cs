using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.Reflection;
using MySql.Data.MySqlClient;

namespace AccountWorking.Tests
{
    [TestClass]
    public sealed class AccountWorkingTest
    {
        private AuthSystem _authSystem;
        private Database _database;

        [TestInitialize]
        public void TestInitialize()
        {
            _authSystem = new AuthSystem();
            _database = new Database();
            CleanDatabase();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            CleanDatabase();
        }

        private void CleanDatabase()
        {
            try
            {
                _database.OpenConnection();
                using (var command = new MySqlCommand("DELETE FROM `users`", _database.GetConnection()))
                {
                    command.ExecuteNonQuery();
                }
                _database.CloseConnection();
            }
            catch (Exception)
            {
                
            }
        }

        [TestMethod]
        public void TestCase_0001_RegisterNewUser_Success()
        {
            // Arrange
            string uniqueLogin = "testuser_1";
            string password = "ValidPass1!";
            string name = "Иван";
            string surname = "Иванов";

            // Act
            var result = _authSystem.Register(uniqueLogin, password, name, surname);

            // Assert
            Assert.AreEqual("Пользователь успешно зарегистрирован", result.message);
        }

        [TestMethod]
        public void TestCase_0002_RegisterExistingUser_Failure()
        {
            // Arrange
            string existingLogin = "existing_user";
            _authSystem.Register(existingLogin, "ValidPass1!", "Петр", "Петров");

            // Act
            var result = _authSystem.Register(existingLogin, "AnotherPass1!", "Иван", "Иванов");

            // Assert
            Assert.IsFalse(result.success);
            Assert.IsTrue(result.message.Contains("уже существует"));
        }

        [TestMethod]
        public void TestCase_0003_RegisterInvalidLogin_Failure()
        {
            // Arrange & Act
            var result = _authSystem.Register("ab", "ValidPass1!", "Анна", "Сидорова");

            // Assert
            Assert.IsFalse(result.success);
            Assert.IsTrue(result.message.Contains("Логин должен содержать"));
        }

        [TestMethod]
        public void TestCase_0004_RegisterWeakPassword_Failure()
        {
            // Arrange & Act
            var result = _authSystem.Register("newuser", "weak", "Анна", "Сидорова");

            // Assert
            Assert.IsFalse(result.success);
            Assert.IsTrue(result.message.Contains("Пароль должен содержать"));
        }

        [TestMethod]
        public void TestCase_0005_LoginValidUser_Success()
        {
            // Arrange
            string login = "test_user";
            string password = "ValidPass1!";
            _authSystem.Register(login, password, "Тест", "Пользователь");

            // Act
            var result = _authSystem.Login(login, password);

            // Assert
            Assert.IsTrue(result.success);
            Assert.AreEqual("Успешный вход", result.message);
        }

        [TestMethod]
        public void TestCase_0006_LoginWrongPassword_Failure()
        {
            // Arrange
            string login = "test_user_wrong_pass";
            string correctPassword = "ValidPass1!";
            _authSystem.Register(login, correctPassword, "Тест", "Пользователь");

            // Act
            var result = _authSystem.Login(login, "WrongPass1!");

            // Assert
            Assert.IsFalse(result.success);
            Assert.AreEqual("Неверный пароль", result.message);
        }

        [TestMethod]
        public void TestCase_0007_LoginNonExistentUser_Failure()
        {
            // Arrange & Act
            var result = _authSystem.Login("nonexistent_user_123", "AnyPass1!");

            // Assert
            Assert.IsFalse(result.success);
            Assert.AreEqual("Пользователь не найден", result.message);
        }

        [TestMethod]
        public void TestCase_0008_UpdatePassword_Success()
        {
            // Arrange
            string login = "test_user_update";
            string oldPassword = "OldValidPass1!";
            string newPassword = "NewValidPass1!";
            _authSystem.Register(login, oldPassword, "Тест", "Обновление");

            // Act
            var updateResult = _authSystem.UpdatePassword(login, newPassword);
            var loginResult = _authSystem.Login(login, newPassword);

            // Assert
            Assert.IsTrue(updateResult.success);
            Assert.AreEqual("Пароль успешно обновлен", updateResult.message);
            Assert.IsTrue(loginResult.success);
            Assert.AreEqual("Успешный вход", loginResult.message);
        }

        [TestMethod]
        public void TestCase_0009_UpdatePasswordNonExistentUser_Failure()
        {
            // Arrange & Act
            var result = _authSystem.UpdatePassword("nonexistent_user_456", "NewValidPass1!");

            // Assert
            Assert.IsFalse(result.success);
            Assert.AreEqual("Пользователь не существует", result.message);
        }

        [TestMethod]
        public void TestCase_0010_DeleteUser_Success()
        {
            // Arrange
            string login = "user_to_delete";
            _authSystem.Register(login, "ValidPass1!", "Удаляемый", "Пользователь");

            // Act
            var deleteResult = _authSystem.DeleteUser(login);
            var existsResult = _authSystem.IsUserExists(login);

            // Assert
            Assert.IsTrue(deleteResult.success);
            Assert.AreEqual("Пользователь успешно удален", deleteResult.message);
            Assert.IsFalse(existsResult.exists);
            Assert.AreEqual("Пользователь не существует", existsResult.message);
        }

        [TestMethod]
        public void TestCase_0011_CheckPasswordStrength_Medium()
        {
            // Arrange & Act
            var result = _authSystem.CheckPasswordStrength("Weak1");

            // Assert
            Assert.AreEqual(3, result.strength); 
            Assert.AreEqual("Средний пароль", result.message);
        }

        [TestMethod]
        public void TestCase_0012_IsUserExists_Success()
        {
            // Arrange
            string login = "existing_user_check";
            _authSystem.Register(login, "ValidPass1!", "Существующий", "Пользователь");

            // Act
            var result = _authSystem.IsUserExists(login);

            // Assert
            Assert.AreEqual("Пользователь существует", result.message);
        }

        [TestMethod]
        public void TestCase_0013_GetUserInfo_Success()
        {
            // Arrange
            string login = "test_user_info";
            string name = "Информация";
            string surname = "Пользователь";
            _authSystem.Register(login, "ValidPass1!", name, surname);

            // Act
            var result = _authSystem.GetUserInfo(login);

            // Assert
            Assert.AreEqual("Данные пользователя получены", result.message);
            Assert.AreEqual(login, result.user["login"]);
            Assert.AreEqual(name, result.user["name"]);
            Assert.AreEqual(surname, result.user["surname"]);
            Assert.AreEqual("***", result.user["password"]);
            Assert.AreEqual("***", result.user["salt"]);
        }

        [TestMethod]
        public void TestCase_0014_GetPasswordRequirements_Success()
        {
            // Arrange & Act
            var result = _authSystem.GetPasswordRequirements();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("Длина: от 8 до 64 символов"));
            Assert.IsTrue(result.Contains("заглавная буква"));
            Assert.IsTrue(result.Contains("строчная буква"));
            Assert.IsTrue(result.Contains("цифра"));
            Assert.IsTrue(result.Contains("специальный символ"));
        }

        [TestMethod]
        public void TestCase_0015_ValidateLogin_PrivateMethod()
        {
            // Arrange
            var method = typeof(AuthSystem).GetMethod("ValidateLogin",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Act & Assert
            Assert.IsFalse((bool)method.Invoke(_authSystem, new object[] { "" }));
            Assert.IsFalse((bool)method.Invoke(_authSystem, new object[] { "ab" }));
            Assert.IsTrue((bool)method.Invoke(_authSystem, new object[] { "valid_user" }));
            Assert.IsFalse((bool)method.Invoke(_authSystem, new object[] { "user@name" }));
        }

        [TestMethod]
        public void TestCase_0016_ValidatePassword_PrivateMethod()
        {
            // Arrange
            var method = typeof(AuthSystem).GetMethod("ValidatePassword",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Act & Assert
            Assert.IsFalse((bool)method.Invoke(_authSystem, new object[] { "" }));
            Assert.IsFalse((bool)method.Invoke(_authSystem, new object[] { "weak" }));
            Assert.IsTrue((bool)method.Invoke(_authSystem, new object[] { "ValidPass1!" }));
            Assert.IsFalse((bool)method.Invoke(_authSystem, new object[] { "NoSpecial1" }));
        }

        [TestMethod]
        public void TestCase_0017_PasswordStrength_VariousLevels()
        {
            // Arrange & Act & Assert
            var emptyResult = _authSystem.CheckPasswordStrength("");
            Assert.AreEqual(0, emptyResult.strength);
            Assert.AreEqual("Пустой пароль", emptyResult.message);

            var weakResult = _authSystem.CheckPasswordStrength("A");
            Assert.AreEqual(1, weakResult.strength);
            Assert.AreEqual("Очень слабый пароль", weakResult.message);

            var mediumResult = _authSystem.CheckPasswordStrength("Aa1");
            Assert.AreEqual(3, mediumResult.strength);
            Assert.AreEqual("Средний пароль", mediumResult.message);

            var strongResult = _authSystem.CheckPasswordStrength("ValidPass1!");
            Assert.AreEqual(5, strongResult.strength);
            Assert.AreEqual("Очень сильный пароль", strongResult.message);
        }

        [TestMethod]
        public void TestCase_0018_UpdatePasswordWeakPassword_Failure()
        {
            // Arrange
            string login = "test_user_weak";
            _authSystem.Register(login, "OldValidPass1!", "Тест", "Пользователь");

            // Act
            var result = _authSystem.UpdatePassword(login, "weak");

            // Assert
            Assert.IsFalse(result.success);
            Assert.IsTrue(result.message.Contains("не соответствует требованиям безопасности"));
        }

        [TestMethod]
        public void TestCase_0019_DeleteNonExistentUser_Failure()
        {
            // Arrange & Act
            var result = _authSystem.DeleteUser("nonexistent_user_999");

            // Assert
            Assert.IsFalse(result.success);
            Assert.AreEqual("Пользователь не существует", result.message);
        }

        [TestMethod]
        public void TestCase_0020_GetUserInfoNonExistentUser_Failure()
        {
            // Arrange & Act
            var result = _authSystem.GetUserInfo("nonexistent_user_info");

            // Assert
            Assert.IsNull(result.user);
            Assert.AreEqual("Пользователь не найден", result.message);
        }
    }
}