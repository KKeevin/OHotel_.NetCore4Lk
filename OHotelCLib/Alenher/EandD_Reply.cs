using System.Security.Cryptography;
using System.Text;

namespace OHotelCLib.Alenher
{
    /// <summary>
    /// 員工密碼加解密
    /// </summary>
    public static class EandD_Reply
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("KsQvD2ROnqFOT6W4!!");
        private static readonly byte[] Iv = Encoding.UTF8.GetBytes("KsQvD2ROnqFOT6W4!!");

        /// <summary>
        /// StaffSet: 密碼加解密
        /// </summary>
        /// <param name="input">輸入字串</param>
        /// <param name="mode">1=加密, 2=解密</param>
        /// <param name="version">版本 (保留)</param>
        /// <returns>處理後的字串</returns>
        public static string StaffSet(string input, int mode, int version)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return mode == 1 ? Encrypt(input) : Decrypt(input);
        }

        private static string Encrypt(string plainText)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = SHA256.HashData(Key);
                aes.IV = MD5.HashData(Iv);
                var encryptor = aes.CreateEncryptor();
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                return Convert.ToBase64String(encrypted);
            }
            catch
            {
                return plainText;
            }
        }

        private static string Decrypt(string cipherText)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = SHA256.HashData(Key);
                aes.IV = MD5.HashData(Iv);
                var decryptor = aes.CreateDecryptor();
                var cipherBytes = Convert.FromBase64String(cipherText);
                var decrypted = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                return cipherText;
            }
        }
    }
}
