using System.Security.Cryptography;
using System.Text;

namespace Utilities;

public static class KeyHelper
{
    public static string GenerateApiKey(int size = 32)
    {
        byte[] keyBytes = new byte[size];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(keyBytes);
        }
        

        return Convert.ToBase64String(keyBytes);
    }

    public static byte[] EncryptString(string plainText, string encryptionKey, string encryptionIV)
    {
        using Aes aesAlg = Aes.Create();
        aesAlg.Key = Convert.FromBase64String(encryptionKey);
        aesAlg.IV = Convert.FromBase64String(encryptionIV);

        ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
        using MemoryStream msEncrypt = new();
        using (CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (StreamWriter swEncrypt = new(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }
        return msEncrypt.ToArray();
    }

    public static string DecryptString(byte[] cipherText, string encryptionKey, string encryptionIV)
    {
        using Aes aesAlg = Aes.Create();
        aesAlg.Key = Convert.FromBase64String(encryptionKey);
        aesAlg.IV = Convert.FromBase64String(encryptionIV);

        ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
        using MemoryStream msDecrypt = new(cipherText);
        using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
        using StreamReader srDecrypt = new(csDecrypt);
        return srDecrypt.ReadToEnd();
    }
}

