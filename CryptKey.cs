// cCrypt.cs
using System;
using System.IO;
using System.Security.Cryptography;

namespace DotStd
{
    public abstract class CryptKey
    {
        // Base for helper mechanism to store key for crypt/decrypt of some string.

        protected byte[] _Key; // kLen bytes.
        protected byte[] _IV; // kLen bytes.
        protected SymmetricAlgorithm _Algo; // what crypto algorithm to use ?

        public string EncryptStr(string value)
        {
            // To Base64 string
            // Can throw. "System.Security.Cryptography.CryptographicException: 'Specified key is not a valid size for this algorithm.'"

            if (string.IsNullOrEmpty(value))
                return "";
            using (var ms = new MemoryStream())
            {
                var cs = new CryptoStream(ms, _Algo.CreateEncryptor(_Key, _IV), CryptoStreamMode.Write);
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(value);
                    sw.Flush();
                    cs.FlushFinalBlock();
                    ms.Flush();
                    //convert back to a string Base64
                    return Convert.ToBase64String(ms.GetBuffer(), (int)0, (int)ms.Length);
                }
            }
        }

        public string DecryptStr(string value)
        {
            // From Base64 string
            if (string.IsNullOrEmpty(value))
                return "";
            try
            {
                //convert from Base64 string to byte array
                byte[] buffer = Convert.FromBase64String(value);
                var ms = new MemoryStream(buffer);
                var cs = new CryptoStream(ms, _Algo.CreateDecryptor(_Key, _IV), CryptoStreamMode.Read);
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
            catch (Exception)
            {
                // OK invalid junk codes just return blank.
                return "";  // invalid junk might not decode ?
            }
        }
    }

    public class cCryptD : CryptKey
    {
        // Standard DES encryption
        // 8 bytes randomly selected for both the Key and the Initialization Vector
        // the IV is used to encrypt the first block of text so that any repetitive patterns are not apparent

        public const int kLen = 8;

        public cCryptD(byte[] k, byte[] i)
        {
            //Assert.IsTrue(k.Length == kLen);
            //Assert.IsTrue(i.Length == kLen);
            _Key = k;
            _IV = i;
            _Algo = new DESCryptoServiceProvider();
        }
    }

    public class cCryptD3 : CryptKey
    {
        // TRIPLE DES encryption, decryption
        // 24 byte or 192 bit key and IV for TripleDES

        public const int kLen = 24;

        public cCryptD3(byte[] k, byte[] i)
        {
            //Assert.IsTrue(k.Length == kLen);
            //Assert.IsTrue(i.Length == kLen);
            _Key = k;
            _IV = i;
            _Algo = new TripleDESCryptoServiceProvider();
        }
    }
}
