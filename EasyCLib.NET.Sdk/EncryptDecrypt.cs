using System.Security.Cryptography;
using System.Text;

namespace EasyCLib.NET.Sdk
{
    public class EncryptDecrypt : IEncryptDecrypt
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public EncryptDecrypt(string? key = null)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key ?? "KsQvD2ROnqFOT6W4");
            using var md5 = MD5.Create();
            _key = md5.ComputeHash(keyBytes);
            _iv = md5.ComputeHash(_key);
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            return Convert.ToBase64String(encrypted);
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;
            try
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;
                var decryptor = aes.CreateDecryptor();
                var cipherBytes = Convert.FromBase64String(cipherText);
                var decrypted = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
