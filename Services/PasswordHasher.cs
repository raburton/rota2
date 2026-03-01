using System.Security.Cryptography;
using System.Text;

namespace Rota2.Services
{
    public static class PasswordHasher
    {
        // PBKDF2
        public static string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, 100_000, HashAlgorithmName.SHA256, 32);
            var result = new byte[1 + salt.Length + hash.Length];
            result[0] = 0; // version
            Buffer.BlockCopy(salt, 0, result, 1, salt.Length);
            Buffer.BlockCopy(hash, 0, result, 1 + salt.Length, hash.Length);
            return Convert.ToBase64String(result);
        }

        public static bool Verify(string hashString, string password)
        {
            try
            {
                var data = Convert.FromBase64String(hashString);
                if (data[0] != 0) return false;
                var salt = new byte[16];
                Buffer.BlockCopy(data, 1, salt, 0, salt.Length);
                var hash = new byte[32];
                Buffer.BlockCopy(data, 1 + salt.Length, hash, 0, hash.Length);
                var testHash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, 100_000, HashAlgorithmName.SHA256, 32);
                return CryptographicOperations.FixedTimeEquals(testHash, hash);
            }
            catch
            {
                return false;
            }
        }
    }
}
