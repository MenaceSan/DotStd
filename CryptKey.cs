// cCrypt.cs
using System;
using System.IO;
using System.Security.Cryptography;

namespace DotStd
{
    /// <summary>
    /// Base for helper mechanism to store key for crypt/decrypt of some string.
    /// </summary>
    public abstract class CryptKey
    {
        protected byte[] _Key; // kLen bytes. pad to size that fits _Algo.KeySize/8
        protected byte[] _IV; // kLen bytes. pad to size that fits _Algo.BlockSize/8
        protected SymmetricAlgorithm _Algo; // what crypto algorithm to use ?

        protected CryptKey(byte[] key, byte[] iv, SymmetricAlgorithm algo)
        {
            _Key = key;
            _IV = iv;
            _Algo = algo;
        }

        public string EncryptStr(string value)
        {
            // To Base64 string. pad out to proper size for algorithm?
            // Can throw. "System.Security.Cryptography.CryptographicException: 'Specified key is not a valid size for this algorithm.'"
            // or Specified initialization vector (IV) does not match the block size for this algorithm. (Parameter 'rgbIV')

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

    /// <summary>
    /// Standard DES encryption.
    /// 8 bytes randomly selected for both the Key and the Initialization Vector.
    /// the IV is used to encrypt the first block of text so that any repetitive patterns are not apparent
    /// </summary>
    public class cCryptD : CryptKey
    {
        public const int kLen = 8;

        public cCryptD(byte[] k, byte[] iv) : base(k, iv, DES.Create())
        {
            //Assert.IsTrue(k.Length == kLen);
            //Assert.IsTrue(i.Length == kLen);
        }
    }

    /// <summary>
    /// TRIPLE DES encryption, decryption.
    /// 24 byte or 192 bit key and IV for TripleDES
    /// </summary>
    public class cCryptD3 : CryptKey
    {
        public const int kLen = 24;

        public cCryptD3(byte[] k, byte[] iv) : base(k, iv, TripleDES.Create())
        {
            //Assert.IsTrue(k.Length == kLen);
            //Assert.IsTrue(i.Length == kLen);
        }
    }
}
