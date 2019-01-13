using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace DotStd
{
    public static class HashUtil
    {
        // Hashes can be used for 1. Id. Avoid Accidental Collision, 2. Security. Avoid intentional collision.

        public static ulong GetKnuthHash(string read)
        {
            // Very fast 64 bit string hash. 
            // like object.GetHashCode() but 64 bit.
            
            ulong hashedValue = 3074457345618258791ul;
            for (int i = 0; i < read.Length; i++)
            {
                hashedValue += read[i];
                hashedValue *= 3074457345618258799ul;
            }
            return hashedValue;
        }

        public static byte[] MakeHashLen(byte[] inp, int lenBin)
        {
            // Adjust the length of a hash to a new length. try not to lose noise.
            // NOTE: might wrap output in StringUtil.ToHexStr() or Convert.ToBase64String()
            // lenBin = return size.

            if (inp.Length == lenBin)   // no change required.
                return inp;

            int j = 0;
            byte[] bout = new byte[lenBin];
            foreach (byte b in inp)
            {
                bout[j] ^= b;
                if (++j >= lenBin) j = 0;
            }

            if (inp.Length < lenBin)
            {
                int i = 0;
                for (; j < lenBin; j++)
                {
                    bout[j] ^= inp[i];
                    if (++i >= inp.Length) i = 0;
                }
            }

            return bout;
        }
    }

    public static class HashMD5
    {
        static ThreadLocal<MD5CryptoServiceProvider> _Hasher = new ThreadLocal<MD5CryptoServiceProvider>(() =>
        {
            // Create a thread local version of this that we can share/re-use ?
            return new MD5CryptoServiceProvider();
        });

        public static MD5CryptoServiceProvider GetHasher()
        {
            // MD5 = Return 128 bits. 16 bytes for a base64 string of 24 chars.
            // NIST recommends SHA-256 or better for passwords.
            // https://stackoverflow.com/questions/247304/what-data-type-to-use-for-hashed-password-field-and-what-length
 
            var hasher = _Hasher.Value;
            hasher.Initialize();
            return hasher;
            // return new MD5CryptoServiceProvider();
        }

        public static byte[] GetHash(string str)
        {
            // Convert the input string to a byte array and compute the hash.
            return GetHasher().ComputeHash(System.Text.Encoding.Default.GetBytes(str));
        }

        public static byte[] GetHash(string str, int salt)
        {
            // NOTE: might wrap output in StringUtil.ToHexStr() or Convert.ToBase64String()
            if (salt > 0)
            {
                str += salt.ToString();   // just append the string.
            }
            return GetHash(str);
        }

        public static byte[] GetHashFile(string filename)
        {
            // MD5 = Return 128 bits. 16 bytes for contents of a file.
            // NOTE: might wrap output in StringUtil.ToHexStr() or Convert.ToBase64String()

            using (var fs = new FileStream(filename, FileMode.Open))
            {
                // Convert the input string to a byte array and compute the hash.
                return GetHasher().ComputeHash(fs);
            }
        }

        public static byte[] MakeHashKey(string str, int salt, int lenBin = 16)
        {
            // Make a hash for a string and change its size arbitrarily.
            // lenBin = return size.
            return HashUtil.MakeHashLen(GetHash(str, salt), lenBin);
        }

        public static string GetHashStr(string str, int salt, int lenBase64 = 24)
        {
            // lenBase64 = how big is the output base64 string.
            int lenBin = SerializeUtil.FromBase64Len(lenBase64);
            return Convert.ToBase64String(MakeHashKey(str, salt, lenBin));
        }
    }

    public static class HashSec
    {
        // Secure hash.

        static ThreadLocal<SHA256CryptoServiceProvider> _Hasher = new ThreadLocal<SHA256CryptoServiceProvider>(() =>
        {
            // Create a thread local version of this that we can share/re-use ?
            return new SHA256CryptoServiceProvider();
        });
     
        public static SHA256CryptoServiceProvider GetHasher()
        {
            // NIST recommends SHA-256 or better for passwords.
            // SHA256 = Return 256 bits. 32 bytes for a base64 string of 44 chars.
            // https://stackoverflow.com/questions/247304/what-data-type-to-use-for-hashed-password-field-and-what-length

            var hasher = _Hasher.Value;
            hasher.Initialize();
            return hasher;
            // return new SHA256CryptoServiceProvider();
        }

        public static byte[] GetHash(string str)
        {
            // Convert the input string to a byte array and compute the hash.
            return GetHasher().ComputeHash(System.Text.Encoding.Default.GetBytes(str));
        }

        public static byte[] GetHash(string str, int salt)
        {
            // NOTE: might wrap output in StringUtil.ToHexStr() or Convert.ToBase64String()
            if (salt > 0)
            {
                str += salt.ToString();   // just append the string.
            }
            return GetHash(str);
        }

        public static byte[] MakeHashKey(string str, int salt, int lenBin = 32)
        {
            // Make a hash for a string and change its size arbitrarily.
            // lenBin = return size.
            return HashUtil.MakeHashLen(GetHash(str, salt), lenBin);
        }

        public static string GetHashStr(string str, int salt, int lenBase64 = 44)
        {
            // lenBase64 = how big is the output base64 string.
            int lenBin = SerializeUtil.FromBase64Len(lenBase64);
            return Convert.ToBase64String(MakeHashKey(str, salt, lenBin));
        }
    }
}
