using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SSHKeyAuth
{

    static class AesEncryptionHelper
    {
        private static readonly string KeyFilePath = "key.dat"; // AES Key Storage
        private static readonly string PassphraseFilePath = "passphrases.csv"; // Encrypted Passphrases Storage

        public static byte[] GenerateAESKey()
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateKey();
                return aes.Key;
            }
        }

        public static void SaveAESKey(byte[] key)
        {
            File.WriteAllBytes(KeyFilePath, key);
        }

        public static byte[] LoadAESKey()
        {
            if (!File.Exists(KeyFilePath))
                throw new FileNotFoundException("AES Key file not found!");

            return File.ReadAllBytes(KeyFilePath);
        }

        public static string EncryptString(string plainText, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    using (var writer = new StreamWriter(cryptoStream))
                    {
                        writer.Write(plainText);
                    }
                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
        }

        public static string DecryptString(string encryptedText, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var memoryStream = new MemoryStream(Convert.FromBase64String(encryptedText)))
                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                using (var reader = new StreamReader(cryptoStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }

}
