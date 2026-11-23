using System;
using System.Security.Cryptography;
using ArrangeMarriage.Application.Interfaces;

namespace ArrangeMarriage.Infrastructure.Services
{
    /// <summary>PBKDF2-HMAC-SHA256 hasher. Format: iterations.salt.hash (base64 parts).</summary>
    public class Pbkdf2PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 210_000; // OWASP guidance for PBKDF2-SHA256

        /// <inheritdoc />
        public string Hash(string password)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentException("Password is required.", nameof(password));

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        }

        /// <inheritdoc />
        public bool Verify(string password, string hash)
        {
            var parts = hash.Split('.');
            if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations)) return false;

            try
            {
                var salt = Convert.FromBase64String(parts[1]);
                var expected = Convert.FromBase64String(parts[2]);
                var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                // Constant-time compare avoids leaking match length through timing.
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (FormatException)
            {
                return false; // Stored value is not in our format (e.g. legacy plaintext): never matches.
            }
        }
    }
}
