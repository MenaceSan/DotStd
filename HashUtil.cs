using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace DotStd
{
    public class HashMD5
    {
        // lenBase64 = 24

        static ThreadLocal<MD5CryptoServiceProvider> _Hasher = new ThreadLocal<MD5CryptoServiceProvider>(() =>
        {
            // Create a thread local version of this that we can share/re-use ?
            return new MD5CryptoServiceProvider();
        });

        public static HashAlgorithm Get()
        {
            // MD5 = Return 128 bits. 16 bytes for a base64 string of 24 chars.
            // NIST recommends SHA-256 or better for passwords.
            // https://stackoverflow.com/questions/247304/what-data-type-to-use-for-hashed-password-field-and-what-length

            var hasher = _Hasher.Value;
            hasher.Initialize();
            return hasher; // return new MD5CryptoServiceProvider();
        }
    }

    public static class HashSHA256
    {
        // Secure hash. SHA256
        //   The SHA256 hash of "Hello World!" is hex "7f83b1657ff1fc53b92dc18148a1d65dfc2d4b1fa3d677284addd200126d9069".
        
        static ThreadLocal<SHA256CryptoServiceProvider> _Hasher = new ThreadLocal<SHA256CryptoServiceProvider>(() =>
        {
            // Create a thread local version of this that we can share/re-use ?
            return new SHA256CryptoServiceProvider();
        });

        public static HashAlgorithm Get()
        {
            // NIST recommends SHA-256 or better for passwords.
            // SHA256 = Return 256 bits. 32 bytes for a base64 string of 44 chars.
            // https://stackoverflow.com/questions/247304/what-data-type-to-use-for-hashed-password-field-and-what-length

            var hasher = _Hasher.Value;
            hasher.Initialize();    // might be shared so init.
            return hasher; // return new SHA256CryptoServiceProvider();
        }
    }

    public static class HashSHA384
    {
        // Secure hash. SHA384

        static ThreadLocal<SHA384CryptoServiceProvider> _Hasher = new ThreadLocal<SHA384CryptoServiceProvider>(() =>
        {
            // Create a thread local version of this that we can share/re-use ?
            return new SHA384CryptoServiceProvider();
        });

        public static SHA384CryptoServiceProvider Get()
        {
            var hasher = _Hasher.Value;
            hasher.Initialize();    // might be shared so init.
            return hasher; // return new SHA384CryptoServiceProvider();
        }
    }

    public class HashUtil
    {
        // Hashes can be used for 1. Id. Avoid Accidental Collision, 2. Security. Avoid intentional collision.
        // NOTE: might wrap byte[] output in StringUtil.ToHexStr() or Convert.ToBase64String()

        private HashAlgorithm _Hasher;

        public void Init()
        {
            _Hasher.Initialize();
        }

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

        public byte[] GetHash(string str)
        {
            // Convert the input string to a byte array and compute the hash.
            return _Hasher.ComputeHash(System.Text.Encoding.Default.GetBytes(str));
        }

        public byte[] GetHash(string str, int salt)
        {
            // NOTE: might wrap output in StringUtil.ToHexStr() or Convert.ToBase64String()
            if (salt > 0)
            {
                str += salt.ToString();   // just append the string.
            }
            return GetHash(str);
        }

        public byte[] GetHashFile(string filename)
        {
            // Hash the contents of a file.
            // MD5 = Return 128 bits. 16 bytes for contents of a file.

            using (var fs = new FileStream(filename, FileMode.Open))
            {
                // Convert the input string to a byte array and compute the hash.
                return _Hasher.ComputeHash(fs);
            }
        }

        public byte[] MakeHashKey(string str, int salt, int lenBin = 16)
        {
            // Make a hash for a string and change its size arbitrarily.
            // lenBin = return size.
            return HashUtil.MakeHashLen(GetHash(str, salt), lenBin);
        }

        public string GetHashStr(string str, int salt, int lenBase64 = 24)
        {
            // lenBase64 = how big is the output base64 string.
            int lenBin = SerializeUtil.FromBase64Len(lenBase64);
            return Convert.ToBase64String(MakeHashKey(str, salt, lenBin));
        }

        public HashUtil(HashAlgorithm hasher)
        {
            _Hasher = hasher;
        }

        public HashUtil(string hashAlgName)
        {
            if (hashAlgName.StartsWith("md5"))
            {
                _Hasher = HashMD5.Get();
            }
            else if (hashAlgName.StartsWith("sha256"))
            {
                _Hasher = HashSHA256.Get();
            }
            else if (hashAlgName.StartsWith("sha384"))
            {
                _Hasher = HashSHA384.Get();
            }
        }
    }
}
