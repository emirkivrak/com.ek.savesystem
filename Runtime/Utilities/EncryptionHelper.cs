using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EK.SaveSystem
{
    /// <summary>
    /// Helper class for AES-256 encryption and decryption operations.
    /// </summary>
    public static class EncryptionHelper
    {
        private const int KeySize = 256;
        private const int BlockSize = 128;
        private const int Iterations = 10000;
        private const int SaltSize = 16;

        /// <summary>
        /// Encrypts plain text using AES-256 encryption.
        /// </summary>
        /// <param name="plainText">The text to encrypt</param>
        /// <param name="passphrase">The encryption key/passphrase</param>
        /// <returns>Base64 encoded encrypted string with salt and IV</returns>
        public static string Encrypt(string plainText, string passphrase)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                SaveSystemLogger.LogError("Cannot encrypt null or empty string");
                return string.Empty;
            }

            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

                // Generate random salt
                byte[] salt = GenerateRandomBytes(SaltSize);

                // Derive key from passphrase
                using (var keyDerivation = new Rfc2898DeriveBytes(passphrase, salt, Iterations))
                {
                    byte[] key = keyDerivation.GetBytes(KeySize / 8);
                    byte[] iv = keyDerivation.GetBytes(BlockSize / 8);

                    using (var aes = new AesManaged())
                    {
                        aes.KeySize = KeySize;
                        aes.BlockSize = BlockSize;
                        aes.Key = key;
                        aes.IV = iv;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;

                        using (var encryptor = aes.CreateEncryptor())
                        using (var ms = new MemoryStream())
                        {
                            // Write salt first
                            ms.Write(salt, 0, salt.Length);

                            // Write encrypted data
                            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                            {
                                cs.Write(plainBytes, 0, plainBytes.Length);
                                cs.FlushFinalBlock();
                            }

                            return Convert.ToBase64String(ms.ToArray());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SaveSystemLogger.LogError($"Encryption failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Decrypts cipher text that was encrypted with the Encrypt method.
        /// </summary>
        /// <param name="cipherText">Base64 encoded encrypted string</param>
        /// <param name="passphrase">The decryption key/passphrase (must match encryption key)</param>
        /// <returns>Decrypted plain text</returns>
        public static string Decrypt(string cipherText, string passphrase)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                SaveSystemLogger.LogError("Cannot decrypt null or empty string");
                return string.Empty;
            }

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);

                using (var ms = new MemoryStream(cipherBytes))
                {
                    // Read salt
                    byte[] salt = new byte[SaltSize];
                    ms.Read(salt, 0, SaltSize);

                    // Derive key from passphrase
                    using (var keyDerivation = new Rfc2898DeriveBytes(passphrase, salt, Iterations))
                    {
                        byte[] key = keyDerivation.GetBytes(KeySize / 8);
                        byte[] iv = keyDerivation.GetBytes(BlockSize / 8);

                        using (var aes = new AesManaged())
                        {
                            aes.KeySize = KeySize;
                            aes.BlockSize = BlockSize;
                            aes.Key = key;
                            aes.IV = iv;
                            aes.Mode = CipherMode.CBC;
                            aes.Padding = PaddingMode.PKCS7;

                            using (var decryptor = aes.CreateDecryptor())
                            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                            using (var resultStream = new MemoryStream())
                            {
                                cs.CopyTo(resultStream);
                                return Encoding.UTF8.GetString(resultStream.ToArray());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SaveSystemLogger.LogError($"Decryption failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Generates cryptographically secure random bytes.
        /// </summary>
        private static byte[] GenerateRandomBytes(int length)
        {
            byte[] bytes = new byte[length];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(bytes);
            }
            return bytes;
        }
    }
}

