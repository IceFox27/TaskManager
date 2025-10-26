using System;
using System.Data;
using AccountWorking;
using System.Collections.Generic;

namespace TestCases
{
    internal class Program
    {
        private static AuthSystem _authSystem;

        static void Main(string[] args)
        {
            Console.WriteLine("=== Тестирование модуля AuthSystem (без БД) ===\n");

            try
            {
                // Создаем тестовую среду
                SetupTestEnvironment();

                // Выполнение тестов
                RunTestCases();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выполнении тестов: {ex.Message}");
            }

            Console.WriteLine("\n=== Тестирование завершено ===");
            Console.ReadKey();
        }

        static void SetupTestEnvironment()
        {
            // Для тестирования без БД используем проверки, которые не требуют подключения
            Console.WriteLine("✅ Тестовая среда инициализирована (без подключения к БД)");
            _authSystem = new AuthSystem();
        }

        static void RunTestCases()
        {
            // Тесты, которые не требуют подключения к БД
            TestCase_0003(); // Регистрация с некорректным логином
            TestCase_0004(); // Регистрация со слабым паролем
            TestCase_0011(); // Проверка силы пароля

            // Тесты валидации данных
            TestValidationMethods();

            // Тесты хеширования паролей
            TestPasswordHashing();

            Console.WriteLine("\n⚠️  Примечание: Тесты, требующие подключения к БД, пропущены");
            Console.WriteLine("Для полного тестирования убедитесь, что:");
            Console.WriteLine("1. Установлен MySQL Server");
            Console.WriteLine("2. Создана база данных 'taskmanager'");
            Console.WriteLine("3. Connection string корректный");
        }

        static void TestCase_0003()
        {
            Console.WriteLine("=== Тест-кейс 0003: Регистрация с некорректным логином ===");

            try
            {
                // Этот тест должен работать без БД, так как проверяется валидация
                var result = _authSystem.Register("ab", "ValidPass1!", "Анна", "Сидорова");

                if (!result.success && result.message.Contains("Логин должен содержать"))
                {
                    Console.WriteLine("✅ ТЕСТ ПРОЙДЕН: " + result.message);
                }
                else
                {
                    Console.WriteLine($"❌ ТЕСТ НЕ ПРОЙДЕН: Ожидалось (false), Получено ({result.success}, '{result.message}')");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ТЕСТ НЕ ПРОЙДЕН: Исключение - {ex.Message}");
            }
            Console.WriteLine();
        }

        static void TestCase_0004()
        {
            Console.WriteLine("=== Тест-кейс 0004: Регистрация со слабым паролем ===");

            try
            {
                var result = _authSystem.Register($"newuser_{Guid.NewGuid().ToString().Substring(0, 8)}", "weak", "Анна", "Сидорова");

                if (!result.success && result.message.Contains("Пароль должен содержать"))
                {
                    Console.WriteLine("✅ ТЕСТ ПРОЙДЕН: " + result.message);
                }
                else
                {
                    Console.WriteLine($"❌ ТЕСТ НЕ ПРОЙДЕН: Ожидалось (false), Получено ({result.success}, '{result.message}')");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ТЕСТ НЕ ПРОЙДЕН: Исключение - {ex.Message}");
            }
            Console.WriteLine();
        }

        static void TestCase_0011()
        {
            Console.WriteLine("=== Тест-кейс 0011: Проверка силы пароля ===");

            try
            {
                var result = _authSystem.CheckPasswordStrength("Weak1");

                if (result.strength == 2 && result.message == "Слабый пароль")
                {
                    Console.WriteLine("✅ ТЕСТ ПРОЙДЕН: " + result.message + " (сила: " + result.strength + ")");
                }
                else
                {
                    Console.WriteLine($"❌ ТЕСТ НЕ ПРОЙДЕН: Ожидалось (2, 'Слабый пароль'), Получено ({result.strength}, '{result.message}')");
                }

                // Дополнительные проверки силы пароля
                TestPasswordStrength("", 0, "Пустой пароль");
                TestPasswordStrength("A", 1, "Очень слабый пароль");
                TestPasswordStrength("Aa", 2, "Слабый пароль");
                TestPasswordStrength("Aa1", 3, "Средний пароль");
                TestPasswordStrength("Aa1!", 4, "Сильный пароль");
                TestPasswordStrength("ValidPass1!", 5, "Очень сильный пароль");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ТЕСТ НЕ ПРОЙДЕН: Исключение - {ex.Message}");
            }
            Console.WriteLine();
        }

        static void TestPasswordStrength(string password, int expectedStrength, string expectedMessage)
        {
            var result = _authSystem.CheckPasswordStrength(password);
            if (result.strength == expectedStrength && result.message == expectedMessage)
            {
                Console.WriteLine($"✅ Проверка пароля '{password}': {result.message} (сила: {result.strength})");
            }
            else
            {
                Console.WriteLine($"❌ Проверка пароля '{password}': Ожидалось ({expectedStrength}, '{expectedMessage}'), Получено ({result.strength}, '{result.message}')");
            }
        }

        static void TestValidationMethods()
        {
            Console.WriteLine("=== Тестирование методов валидации ===");

            // Тестирование валидации логина
            TestLoginValidation("", false, "Пустой логин");
            TestLoginValidation("ab", false, "Короткий логин");
            TestLoginValidation("valid_user", true, "Валидный логин");
            TestLoginValidation("user@name", false, "Логин с запрещенными символами");
            TestLoginValidation("very_long_username_that_exceeds_maximum_length_123456789", false, "Длинный логин");

            // Тестирование валидации пароля
            TestPasswordValidation("", false, "Пустой пароль");
            TestPasswordValidation("weak", false, "Слабый пароль");
            TestPasswordValidation("ValidPass1!", true, "Валидный пароль");
            TestPasswordValidation("NoSpecial1", false, "Пароль без спецсимволов");
            TestPasswordValidation("noupper1!", false, "Пароль без заглавных");
            TestPasswordValidation("NOLOWER1!", false, "Пароль без строчных");
            TestPasswordValidation("NoDigits!", false, "Пароль без цифр");

            // Тестирование валидации имени
            TestNameValidation("", false, "Пустое имя");
            TestNameValidation("Я", false, "Короткое имя");
            TestNameValidation("Иван", true, "Валидное имя");
            TestNameValidation("Anna-Maria", true, "Имя с дефисом");
            TestNameValidation("John123", false, "Имя с цифрами");
            Console.WriteLine();
        }

        static void TestLoginValidation(string login, bool expected, string description)
        {
            // Используем рефлексию для вызова приватного метода
            try
            {
                var method = typeof(AuthSystem).GetMethod("ValidateLogin",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                bool result = (bool)method.Invoke(_authSystem, new object[] { login });

                if (result == expected)
                {
                    Console.WriteLine($"✅ {description}: {result}");
                }
                else
                {
                    Console.WriteLine($"❌ {description}: Ожидалось {expected}, Получено {result}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {description}: Ошибка - {ex.Message}");
            }
        }

        static void TestPasswordValidation(string password, bool expected, string description)
        {
            try
            {
                var method = typeof(AuthSystem).GetMethod("ValidatePassword",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                bool result = (bool)method.Invoke(_authSystem, new object[] { password });

                if (result == expected)
                {
                    Console.WriteLine($"✅ {description}: {result}");
                }
                else
                {
                    Console.WriteLine($"❌ {description}: Ожидалось {expected}, Получено {result}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {description}: Ошибка - {ex.Message}");
            }
        }

        static void TestNameValidation(string name, bool expected, string description)
        {
            try
            {
                var method = typeof(AuthSystem).GetMethod("ValidateName",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                bool result = (bool)method.Invoke(_authSystem, new object[] { name });

                if (result == expected)
                {
                    Console.WriteLine($"✅ {description}: {result}");
                }
                else
                {
                    Console.WriteLine($"❌ {description}: Ожидалось {expected}, Получено {result}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {description}: Ошибка - {ex.Message}");
            }
        }

        static void TestPasswordHashing()
        {
            Console.WriteLine("=== Тестирование хеширования паролей ===");

            try
            {
                // Используем рефлексию для тестирования приватных методов
                var generateSaltMethod = typeof(AuthSystem).GetMethod("GenerateSalt",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var hashPasswordMethod = typeof(AuthSystem).GetMethod("HashPassword",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var verifyPasswordMethod = typeof(AuthSystem).GetMethod("VerifyPassword",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                // Генерация соли
                string salt1 = (string)generateSaltMethod.Invoke(_authSystem, null);
                string salt2 = (string)generateSaltMethod.Invoke(_authSystem, null);

                if (!string.IsNullOrEmpty(salt1) && !string.IsNullOrEmpty(salt2) && salt1 != salt2)
                {
                    Console.WriteLine("✅ Генерация соли: Соли уникальны и не пусты");
                }
                else
                {
                    Console.WriteLine("❌ Генерация соли: Соли идентичны или пусты");
                }

                // Хеширование пароля
                string password = "TestPass123!";
                string hash1 = (string)hashPasswordMethod.Invoke(_authSystem, new object[] { password, salt1 });
                string hash2 = (string)hashPasswordMethod.Invoke(_authSystem, new object[] { password, salt1 });

                if (!string.IsNullOrEmpty(hash1) && hash1 == hash2)
                {
                    Console.WriteLine("✅ Хеширование пароля: Хеши детерминированы и не пусты");
                }
                else
                {
                    Console.WriteLine("❌ Хеширование пароля: Хеши различаются или пусты");
                }

                // Проверка пароля
                bool verifyResult = (bool)verifyPasswordMethod.Invoke(_authSystem, new object[] { password, salt1, hash1 });
                bool verifyWrongResult = (bool)verifyPasswordMethod.Invoke(_authSystem, new object[] { "WrongPass", salt1, hash1 });

                if (verifyResult && !verifyWrongResult)
                {
                    Console.WriteLine("✅ Проверка пароля: Корректный пароль принимается, неверный отклоняется");
                }
                else
                {
                    Console.WriteLine("❌ Проверка пароля: Ошибка в верификации");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Тестирование хеширования: Ошибка - {ex.Message}");
            }
            Console.WriteLine();
        }
    }
}