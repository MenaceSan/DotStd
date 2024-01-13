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

        /// <summary>
        /// Encrypt a string and return the base64 of the crypt.
        /// pad out to proper size for algorithm?
        /// </summary>
        /// <param name="value">base64</param>
        /// <returns></returns>
        public string EncryptStr(string value)
        {
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

        /// <summary>
        /// Decrypt From Base64 string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>        
        public string DecryptStr(string value)
        {
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
    public class CryptD : CryptKey
    {
        public const int kLen = 8;

        public CryptD(byte[] k, byte[] iv) : base(k, iv, DES.Create())
        {
            //Assert.IsTrue(k.Length == kLen);
            //Assert.IsTrue(i.Length == kLen);
        }
    }

    /// <summary>
    /// TRIPLE DES encryption, decryption.
    /// 24 byte or 192 bit key and IV for TripleDES
    /// </summary>
    public class CryptD3 : CryptKey
    {
        public const int kLen = 24;

        public CryptD3(byte[] k, byte[] iv) : base(k, iv, TripleDES.Create())
        {
            //Assert.IsTrue(k.Length == kLen);
            //Assert.IsTrue(i.Length == kLen);
        }
    }
}
