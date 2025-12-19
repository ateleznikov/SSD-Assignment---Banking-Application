using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace SSD_Assignment___Banking_Application
{
    public static class SecurityHelper
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("A!9hz$81#mPq2&dFv5*L9@zXc4!mNs7Q");
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("HR$2pIj5*d8@!qSz");

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.IV = IV;
                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(cipherText)))
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader sr = new StreamReader(cs))
                            {
                                return sr.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch
            {
                return "Decryption Failed";
            }
        }
    }
}
