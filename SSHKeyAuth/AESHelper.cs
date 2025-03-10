using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Security.Cryptography;

namespace SSHKeyAuth
{
    

    public static class AESHelper
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("Your32ByteLongSuperSecretKey!"); // Change this

        public static string Encrypt(string plainText)
        {
            using Aes aes = Aes.Create();
            aes.Key = Key;
            aes.GenerateIV(); // New IV for each encryption
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using MemoryStream ms = new();
            using CryptoStream cs = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            using BinaryWriter writer = new(cs);

            writer.Write(plainText);
            cs.FlushFinalBlock();

            byte[] ivAndCipherText = new byte[aes.IV.Length + ms.Length];
            Array.Copy(aes.IV, ivAndCipherText, aes.IV.Length);
            Array.Copy(ms.ToArray(), 0, ivAndCipherText, aes.IV.Length, ms.Length);

            return Convert.ToBase64String(ivAndCipherText);
        }

        public static string Decrypt(string encryptedText)
        {
            byte[] fullCipher = Convert.FromBase64String(encryptedText);
            byte[] iv = new byte[16]; // AES block size = 16 bytes
            byte[] cipherText = new byte[fullCipher.Length - iv.Length];

            Array.Copy(fullCipher, iv, iv.Length);
            Array.Copy(fullCipher, iv.Length, cipherText, 0, cipherText.Length);

            using Aes aes = Aes.Create();
            aes.Key = Key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using MemoryStream ms = new(cipherText);
            using CryptoStream cs = new(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using StreamReader reader = new(cs);

            return reader.ReadToEnd();
        }
    }

}
