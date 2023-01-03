using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotStd
{
    /// <summary>
    /// Fast 64 bit hash. ulong output.
    /// like GetKnuthHash
    /// https://stackoverflow.com/questions/8820399/c-sharp-4-0-how-to-get-64-bit-hash-code-of-given-string
    /// https://github.com/brandondahler/Data.HashFunction/blob/master/src/System.Data.HashFunction.xxHash/xxHash_Implementation.cs
    /// </summary>
    public class HashXXH64 : HashAlgorithm
    {
        public ulong Value { get; private set; }   // output short cut.

        public override void Initialize()
        {
            // TODO
            throw new NotImplementedException();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            throw new NotImplementedException();
        }

        protected override byte[] HashFinal()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Wrapper for HashAlgorithm for use in crypto . 
    /// Hashes can be used for 1. Id. Avoid Accidental Collision, 2. Security. Avoid intentional collision.
    /// Crypto hashes are usually on strings (passwords). Actual Hash is on String.ToByteArray().
    /// If we assume a unique salt per user and >= 128 bits of hash, the best attack left is dictionary/brute force password guessing. 
    /// Length of the hash beyond ~256 bits is no longer useful. we need to make it expensive for the attacker.
    /// Algorithms: PDFKDF2 = weak to FPGA usage. scrypt newer. control cost of attack.
    /// NOTE: might wrap byte[] hash output in StringUtil.ToHexStr() or Convert.ToBase64String() for db storage as string ?
    /// https://www.srihash.org/
    /// </summary>
    public class HashUtil
    {
        private HashAlgorithm _Hasher;

        public void Init()
        {
            _Hasher.Initialize();
        }

        public const ulong kHashValue1 = 3074457345618258791ul;
        public const ulong kHashValue2 = 3074457345618258799ul;

        public static ulong GetKnuthHash(string read)
        {
            // Very fast 64 bit string anti-collision hash of a string. 
            // like object.GetHashCode() but 64 bit. Not cryptographically secure.
            // use 64 bits for lower hash collisions. 
            // ? Make this faster by doing 8 byte chunks?

            if (read == null)
                return 0;
            ulong hashedValue = kHashValue1;
            for (int i = 0; i < read.Length; i++)
            {
                hashedValue += read[i];
                hashedValue *= kHashValue2;
            }
            return hashedValue;
        }

        public async Task<byte[]> GetHashStreamAsync(Stream stream)
        {
            // like HashAlgorithm.ComputeHash(Stream) but async

            var buffer = new byte[8192];
            int bytesRead = 0;

            // compute the hash on 8KiB blocks
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                _Hasher.TransformBlock(buffer, 0, bytesRead, buffer, 0);

            _Hasher.TransformFinalBlock(buffer, 0, bytesRead);
            ValidState.ThrowIfNull(_Hasher.Hash, nameof(_Hasher.Hash));
            return _Hasher.Hash;
        }

        public byte[] GetHashFile(string filename)
        {
            // Hash the contents of a file with _Hasher.
            // MD5 = Return 128 bits. 16 bytes for contents of a file. Not crypto. just anti-collision.
            // use SHA512 for true crypto.

            using (var fs = File.OpenRead(filename))
            {
                // Convert the input string to a byte array and compute the hash.
                return _Hasher.ComputeHash(fs);
            }
        }

        public async Task<byte[]> GetHashFileAsync(string filename)
        {
            using (var fs = File.OpenRead(filename))
            {
                // Convert the input string to a byte array and compute the hash.
                return await GetHashStreamAsync(fs);
            }
        }

        public static void MergeHash(byte[] inp, byte[] bout)
        {
            // Combine 2 hashes. try not to lose noise/entropy. xor wrapping extra data (or padding output).
            int j = 0;
            foreach (byte b in inp)
            {
                bout[j] ^= b;
                if (++j >= bout.Length) j = 0;   // wrap back to start. wrap extra data back over previous data.
            }

            if (inp.Length < bout.Length)    // pad output.
            {
                int i = 0;
                for (; j < bout.Length; j++) // fill bout
                {
                    bout[j] ^= inp[i];
                    if (++i >= inp.Length) i = 0;   // wrap to fill bout
                }
            }
        }

        public static byte[] MakeHashLen(byte[] inp, int lenOut)
        {
            // Adjust the length of a hash.
            // lenOut = return byte[] size.

            if (inp.Length == lenOut)   // no change required.
                return inp; // inp is ok the way it is.
            byte[] bout = new byte[lenOut];     // assume 0 init.
            MergeHash(inp, bout);
            return bout;
        }

        public byte[] GetHash(string str)
        {
            // Convert the input string to a byte array and compute the hash.
            // Assume any salt has already been added.

            return _Hasher.ComputeHash(str.ToByteArray());
        }

        public byte[] GetHash(string password, string systemsecret, ulong salt, int id)
        {
            // crypto hashes are on strings (passwords) SecureString
            // Compute a hash of (system secret password + password + ulong random salt + id of user). 
            // cant share lookup attacks across users or pre-compute.
            // all attacks are per user.

            return GetHash(string.Concat(password, systemsecret, salt.ToString(), id));
        }

        public byte[] GetHashSized(string password, string systemsecret, ulong salt, int id, int lenOutBin = 16)
        {
            // Make a arbitrarily sized hash for a password. for db storage.
            // lenOutBin = return size.
            return HashUtil.MakeHashLen(GetHash(password, systemsecret, salt, id), lenOutBin);
        }

        public string GetHashBase64(string password, string systemsecret, ulong salt, int id, int lenOutBase64 = 24)
        {
            // Get Base64 Hash for password. for db storage.
            // leOutnBase64 = how big is the output base64 string. for db storage.
            int lenOutBin = SerializeUtil.FromBase64Len(lenOutBase64);
            return Convert.ToBase64String(GetHashSized(password, systemsecret, salt, id, lenOutBin));
        }

        public static int GetHashInt(ulong n)
        {
            return (int)(n ^ (n >> 32));   // Collapse Hash code.
        }

        public HashUtil(HashAlgorithm hasher)
        {
            // Wrap a hash algorithm.
            _Hasher = hasher;
        }

        public static HashAlgorithm GetMD5()
        {
            // faster. less secure. for low collision hashes.
            // used for new account sign up + user email.
            // MD5 = Return 128 bits. 16 bytes for a base64 string of 24 chars.
            // Wrap a MD5 HashAlgorithm.
            // NIST recommends SHA-256 or better for passwords.
            // https://stackoverflow.com/questions/247304/what-data-type-to-use-for-hashed-password-field-and-what-length
            return MD5.Create();
        }
        public static HashAlgorithm GetSHA256()
        {
            // Secure hash. SHA256
            //   The SHA256 hash of "Hello World!" is hex "7f83b1657ff1fc53b92dc18148a1d65dfc2d4b1fa3d677284addd200126d9069".
            // NIST recommends SHA-256 or better for passwords.
            // SHA256 = Return 256 bits. 32 bytes for a base64 string of 44 chars.
            // https://stackoverflow.com/questions/247304/what-data-type-to-use-for-hashed-password-field-and-what-length

            return SHA256.Create();
        }
        public static HashAlgorithm GetSHA512()
        {
            // secure enough for crypto. 
            // SHA512 = Return 512 bits. 64 bytes for a base64 string of ?? chars.
            // assume >= 64 bit random salt is added to this.
            return SHA512.Create();
        }

        public const string kMd5 = "md5";
        public const string kSha256 = "sha256";

        public static HashAlgorithm FindHasherByName(string hashAlgName)
        {
            // Lookup hasher by name.
            // like static HashAlgorithm.Create(string hashName);
            // HMACSHA256 ? HMACSHA512 ?

            if (hashAlgName.StartsWith(kMd5))
            {
                return GetMD5();
            }
            else if (hashAlgName.StartsWith(kSha256))
            {
                return GetSHA256();
            }
            else if (hashAlgName.StartsWith("sha384"))
            {
                // Secure hash. 
                // SHA384 = Return 384 bits. 48 bytes for a base64 string of ??? chars.

                return SHA384.Create();
            }
            else if (hashAlgName.StartsWith("sha512"))
            {
                // Secure hash. "sha512-Wkxbeuy81yHqZNrMurMURCOCMzkJqaFYnvToublHiOGoVXQ2DS1lOUjKwstbe0GwELrRb9sicdV2y6GiAnVxuw=="
                // SHA512 = Return 512 bits. 64 bytes for a base64 string of ??? chars.

                return GetSHA512();
            }

            throw new ArgumentException("FindHasherByName invalid name");
        }
    }
}
