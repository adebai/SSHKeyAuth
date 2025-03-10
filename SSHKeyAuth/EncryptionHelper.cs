using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

namespace SSHKeyAuth
{
    public static class EncryptionHelper
    {
        private static readonly string keyFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SSHKeyAuth", "key.dat");

        public static byte[] GetOrCreateEncryptionKey()
        {
            if (File.Exists(keyFilePath))
            {
                byte[] encryptedKey = File.ReadAllBytes(keyFilePath);
                return ProtectedData.Unprotect(encryptedKey, null, DataProtectionScope.CurrentUser);
            }
            else
            {
                byte[] newKey = new byte[32]; // AES-256 requires a 32-byte key
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(newKey);
                }

                byte[] encryptedKey = ProtectedData.Protect(newKey, null, DataProtectionScope.CurrentUser);
                Directory.CreateDirectory(Path.GetDirectoryName(keyFilePath));
                File.WriteAllBytes(keyFilePath, encryptedKey);
                return newKey;
            }
        }

        private static byte[] GenerateRandomKey()
        {
            byte[] key = new byte[32]; // AES-256 requires exactly 32 bytes
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return key;
        }
    }
}
