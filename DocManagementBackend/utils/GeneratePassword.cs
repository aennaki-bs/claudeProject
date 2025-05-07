using System;
using System.Linq;
using System.Security.Cryptography;

 namespace DocManagementBackend.utils
{
    public class PasswordGenerator
    {
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        private static readonly char[] LowercaseLetters = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
        private static readonly char[] UppercaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private static readonly char[] Digits = "0123456789".ToCharArray();
        private static readonly char[] SpecialCharacters = "!@#$%^&*()-_=+[]{}|;:,.<>/?".ToCharArray();

        public static string GenerateRandomPassword(int length = 12)
        {
            if (length < 8)
                throw new ArgumentException("Password length must be at least 8 characters.");

            var passwordChars = new char[length];
            passwordChars[0] = GetRandomChar(LowercaseLetters);
            passwordChars[1] = GetRandomChar(UppercaseLetters);
            passwordChars[2] = GetRandomChar(Digits);
            passwordChars[3] = GetRandomChar(SpecialCharacters);

            var allCharacters = LowercaseLetters
                .Concat(UppercaseLetters)
                .Concat(Digits)
                .Concat(SpecialCharacters)
                .ToArray();

            for (int i = 4; i < length; i++)
                passwordChars[i] = GetRandomChar(allCharacters);

            Shuffle(passwordChars);

            return new string(passwordChars);
        }

        private static char GetRandomChar(char[] characters) {
            byte[] randomNumber = new byte[1];
            Rng.GetBytes(randomNumber);
            return characters[randomNumber[0] % characters.Length];
        }

        private static void Shuffle(char[] array) {
            int n = array.Length;
            while (n > 1)
            {
                byte[] randomNumber = new byte[1];
                Rng.GetBytes(randomNumber);
                int k = randomNumber[0] % n;
                n--;
                char value = array[k];
                array[k] = array[n];
                array[n] = value;
            }
        }
    }
}