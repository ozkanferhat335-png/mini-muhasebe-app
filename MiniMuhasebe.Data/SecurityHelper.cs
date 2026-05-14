using System;
using System.Security.Cryptography;
using System.Text;

namespace MiniMuhasebe.Data
{
    /// <summary>
    /// Şifre hash ve doğrulama işlemleri
    /// </summary>
    public class PasswordHelper
    {
        private const int SaltSize = 16; // 128 bit
        private const int HashSize = 20; // 160 bit
        private const int Iterations = 10000;

        /// <summary>
        /// Şifreyi hash'ler ve salt oluşturur
        /// </summary>
        public static (string hash, string salt) HashPassword(string password)
        {
            // Random salt oluştur
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] saltBytes = new byte[SaltSize];
                rng.GetBytes(saltBytes);
                string saltString = Convert.ToBase64String(saltBytes);

                // Hash oluştur
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256))
                {
                    byte[] hash = pbkdf2.GetBytes(HashSize);
                    string hashString = Convert.ToBase64String(hash);

                    return (hashString, saltString);
                }
            }
        }

        /// <summary>
        /// Şifrenin hash'i ile eşleşip eşleşmediğini kontrol eder
        /// </summary>
        public static bool VerifyPassword(string password, string hash, string salt)
        {
            try
            {
                byte[] saltBytes = Convert.FromBase64String(salt);
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256))
                {
                    byte[] computedHash = pbkdf2.GetBytes(HashSize);
                    string computedHashString = Convert.ToBase64String(computedHash);
                    return computedHashString == hash;
                }
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Duyarlı veri şifreleme/şifre çözme işlemleri (API anahtarları vb.)
    /// </summary>
    public class EncryptionHelper
    {
        private readonly string _encryptionKey;

        public EncryptionHelper(string encryptionKey)
        {
            if (string.IsNullOrEmpty(encryptionKey))
                throw new ArgumentException("Şifreleme anahtarı boş olamaz.");
            _encryptionKey = encryptionKey;
        }

        /// <summary>
        /// Veriyi şifreler
        /// </summary>
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                using (var aes = Aes.Create())
                {
                    // Anahtarı düzelt (256-bit = 32 byte)
                    byte[] key = Encoding.UTF8.GetBytes(_encryptionKey);
                    Array.Resize(ref key, 32);

                    aes.Key = key;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                        byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                        // IV + Encrypted Data
                        byte[] result = new byte[aes.IV.Length + encryptedBytes.Length];
                        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
                        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

                        return Convert.ToBase64String(result);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Şifreleme hatası: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Şifreli veriyi çözer
        /// </summary>
        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                using (var aes = Aes.Create())
                {
                    // Anahtarı düzelt
                    byte[] key = Encoding.UTF8.GetBytes(_encryptionKey);
                    Array.Resize(ref key, 32);

                    aes.Key = key;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    byte[] cipherBytes = Convert.FromBase64String(cipherText);
                    byte[] iv = new byte[aes.IV.Length];
                    Array.Copy(cipherBytes, 0, iv, 0, iv.Length);

                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    {
                        byte[] encryptedBytes = new byte[cipherBytes.Length - iv.Length];
                        Array.Copy(cipherBytes, iv.Length, encryptedBytes, 0, encryptedBytes.Length);

                        byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Şifre çözme hatası: {ex.Message}", ex);
            }
        }
    }
}
