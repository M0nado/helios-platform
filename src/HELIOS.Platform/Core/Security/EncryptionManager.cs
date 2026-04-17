using System;
using System.Security.Cryptography;
using System.Text;

namespace HELIOS.Platform.Core.Security
{
    /// <summary>
    /// Encryption and decryption manager for vault operations
    /// </summary>
    public class EncryptionManager
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public EncryptionManager()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                _key = new byte[32];
                _iv = new byte[16];
                rng.GetBytes(_key);
                rng.GetBytes(_iv);
            }
        }

        /// <summary>
        /// Encrypt data using AES-256
        /// </summary>
        public byte[] Encrypt(string plainText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    var plainBytes = Encoding.UTF8.GetBytes(plainText);
                    return encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                }
            }
        }

        /// <summary>
        /// Decrypt data using AES-256
        /// </summary>
        public string Decrypt(byte[] cipherText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    var decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }

        /// <summary>
        /// Generate hash for password verification
        /// </summary>
        public string GenerateHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        /// <summary>
        /// Verify password against hash
        /// </summary>
        public bool VerifyHash(string input, string hash)
        {
            var hashOfInput = GenerateHash(input);
            return hashOfInput == hash;
        }
    }
}
