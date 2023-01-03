using System;
using System.Collections.Generic;

namespace DotStd
{
    /// <summary>
    /// A potentially unlimited large size bit mask serialized as a base64 string, defining options on/off.
    /// NOTE: Bit positions are ZERO BASED. bit 0 = the first bit.
    /// Similar to System.Collections.BitArray
    /// </summary>
    public class BitMask
    {
        public const int kBitsPer = 8;          // bits per char/byte stored in _binary.
        public const int kDefaultSize = 256;    // n bits.

        protected byte[] _binary;    // The raw bytes of the bitmap of unlimited length.

        public BitMask(byte[] binary)
        {
            _binary = binary;   // clone it ?
        }

        int LengthBytes => _binary.Length;
        bool IsEmpty => _binary.Length == 0;

        /// <summary>
        /// Initialize via Base64 string
        /// Can throw "System.FormatException: 'Invalid length for a Base-64 char array or string.'"
        /// use IsValidBase64 ?
        /// TODO add argument maxBits to allocate extra size??
        /// </summary>
        /// <param name="base64String"></param>
        public BitMask(string? base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String))
            {
                // empty means no bits.
                _binary = Array.Empty<byte>();
            }
            else if (base64String.StartsWith("0x"))
            {
                _binary = SerializeUtil.FromHexStr(base64String.Substring(2));
            }
            else if (SerializeUtil.IsValidBase64(base64String))
            {
                _binary = Convert.FromBase64String(base64String);       // will throw if string is bad
            }
            else
            {
                _binary = Array.Empty<byte>();
            }
        }

        /// <summary>
        /// is a single bit set?
        /// </summary>
        /// <param name="bitPos">bit position in the _binary. zero base</param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public bool IsSet(int bitPos, bool defaultValue = false)
        {
            if (IsEmpty)
                return false;    // special
            if (bitPos < 0)    // zero base 
                return false;

            int bytePos = bitPos / kBitsPer;
            if (bytePos < 0 || bytePos >= LengthBytes)
            {
                // If bitPos length is greater then the token length just return defaultValue.
                return defaultValue;
            }

            int bitshift = bitPos % kBitsPer;

            // Get the byte from the byte Array that holds the bit
            // Move the bit to the LeastSignificantPosition (First Position) in the container
            int result = _binary[bytePos] >> bitshift;

            // Check whether the bit value is 1 or 0
            // If the value is 0 return false //User is not authorized to use the module
            //  else if the value is 1 return true. //User has rights to use the module
            return (result & 1) != 0; // to bool
        }

        /// <summary>
        /// Get Array of set bits a generic of type (int)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> GetSetList<T>()
        {
            var list = new List<T>();
            for (int bytePos = 0; bytePos < LengthBytes; bytePos++)
            {
                byte val = _binary[bytePos];
                for (int bitshift = 0; bitshift < kBitsPer; bitshift++)
                {
                    if ((val & 1) != 0)
                    {
                        list.Add((T)(object)((bytePos * kBitsPer) + bitshift));
                    }
                    val >>= 1;
                }
            }
            return list;
        }

        /// <summary>
        /// Make Base64 string from bitmask.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (IsEmpty)
                return string.Empty;
            var b = ByteUtil.TruncateZeros(_binary);
            if (b == null)
                return string.Empty;
            return Convert.ToBase64String(b);
        }

        /// <summary>
        /// make BitMask set to all defaultValue bits.
        /// </summary>
        /// <param name="maxBits"></param>
        /// <param name="defaultValue"></param>
        public BitMask(int maxBits = kDefaultSize, bool defaultValue = false)
        {
            _binary = Array.Empty<byte>();
            SetBitMask(maxBits, defaultValue);
        }

        /// <summary>
        /// get number of bytes to hold this number of bits.
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>
        static int GetByteCount(int bits)
        {
            return (bits + kBitsPer - 1) / kBitsPer;   // account for odd number of bits.
        }

        /// <summary>
        /// Set/Init the bitmask with an int. (obviously limited range)
        /// </summary>
        /// <param name="maxBits"></param>
        /// <param name="defaultValue"></param>
        /// <exception cref="OutOfMemoryException"></exception>
        public void SetBitMask(int maxBits = kDefaultSize, bool defaultValue = false)
        {
            byte bdef = (byte)(defaultValue ? 0xff : 0x0);

            int countBytes = GetByteCount(maxBits);
            _binary = new byte[countBytes];  // Is this right ??
            ValidState.ThrowIfNull(_binary, "BitMask OutOfMemory");

            for (int i = 0; i < countBytes; i++) // fill with valueDef
                _binary[i] = bdef;

            // Make sure the last byte is ok.
            int bitsLast = maxBits % kBitsPer;
            if (bitsLast != 0)
            {
                // fix it.
                _binary[countBytes - 1] = (byte)((1 << bitsLast) - 1);
            }
        }

        /// <summary>
        /// Set state of a single bit.
        /// </summary>
        /// <param name="bitPos">bit position in the _binary. 0 based.</param>
        /// <param name="bitValue">bool</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetBit(int bitPos, bool bitValue = true)
        {
            if (bitPos < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bitPos), bitPos, "BitMask bitPos negative");
                // return;
            }

            int bytePos = bitPos / kBitsPer;
            if (IsEmpty)
            {
                SetBitMask(Math.Max(kDefaultSize, bytePos + 1), false);
            }
            else if (bytePos >= LengthBytes)
            {
                // Auto grow. If bitPos length is greater then the max length .  
                var binary2 = new byte[bytePos + 1];
                Buffer.BlockCopy(_binary, 0, binary2, 0, LengthBytes);
                _binary = binary2;
            }

            int bitshift = bitPos % kBitsPer;

            byte x = _binary[bytePos];
            if (bitValue)
            {
                x |= (byte)(1 << bitshift);
            }
            else
            {
                x &= (byte)~(1 << bitshift);
            }
            _binary[bytePos] = x;
        }

        /// <summary>
        /// Combine these bits. Or bits.
        /// </summary>
        /// <param name="bits"></param>
        public void OpOr(BitMask bits)
        {
            if (bits.IsEmpty)
                return;
            int bytes1 = bits.LengthBytes;
            if (bytes1 <= 0)
                return;
            int bytes2 = (IsEmpty) ? 0 : LengthBytes;
            if (bytes2 < bytes1)
            {
                Array.Resize(ref _binary, bytes1);
            }
            for (int i = 0; i < bytes1; i++)
            {
                _binary[i] |= bits._binary[i];
            }
        }

        /// <summary>
        /// Creates a new bitmask with all bits set to defaultBitValue.
        /// </summary>
        /// <param name="maxBits"></param>
        /// <param name="defaultBitValue"></param>
        /// <returns></returns>
        public static string CreateBitMask(int maxBits = kDefaultSize, bool defaultBitValue = false)
        {
            var t = new BitMask(maxBits, defaultBitValue);
            return t.ToString();
        }

        /// <summary>
        /// Create a new bitmask with one bit changed.
        /// </summary>
        /// <param name="bitPos">bit position in the base64String to set. zero based.</param>
        /// <param name="bitValue"></param>
        /// <param name="base64String"></param>
        /// <returns></returns>
        public static string CreateBitMask2(int bitPos, bool bitValue, string? base64String)
        {
            var t = new BitMask(base64String);
            t.SetBit(bitPos, bitValue);
            return t.ToString();
        }

        /// <summary>
        /// is bit set?
        /// </summary>
        /// <param name="bitPos">bit position in the base64String</param>
        /// <param name="base64String"></param>
        /// <returns></returns>
        public static bool IsBitSet(int bitPos, string base64String)
        {
            var t = new BitMask(base64String);
            return t.IsSet(bitPos);
        }

        // TODO public void OpAndNot(BitMask bits)  // Turn off bits that are indicated.
    }
}
