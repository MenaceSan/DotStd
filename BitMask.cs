using System;

namespace DotStd
{
    public class BitMask
    {
        // A potentially unlimited large bit mask stored as a base64 string, defining options on/off.
        // Similar to System.Collections.BitArray

        public const int kBitsPer = 8;          // bits per char/byte stored in _binary.
        public const int kFirstBit = 1;         // 1 based bit index for API calls. SetBit(),
        public const int kDefaultSize = 256;    // n bits.

        protected byte[] _binary;    // The raw bytes of the bitmap of unlimited length.

        public BitMask(string base64String)
        {
            // init via Base64 string
            // Can throw "System.FormatException: 'Invalid length for a Base-64 char array or string.'"
            // use IsValidBase64 ?
            // TODO Allocate extra size ??

            if (string.IsNullOrWhiteSpace(base64String))
            {
                // empty means no bits.
                _binary = null;
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
                _binary = null;
            }
        }

        public BitMask(int maxBits = kDefaultSize, bool defaultValue = false)
        {
            // make BitMask set to all defaultValue bits.
            SetBitMask(maxBits, defaultValue);
        }

        static int GetByteCount(int bits)
        {
            // get number of bytes to hold this number of bits.
            return 1 + ((bits) / kBitsPer);   // always add an extra just in case.
        }

        public void SetBitMask(int maxBits = kDefaultSize, bool defaultValue = false)
        {
            // Set/Init the bitmask with an int. (obviously limited range)
            int count = GetByteCount(maxBits);
            byte bdef = (byte)(defaultValue ? 0xff : 0x0);
            _binary = new byte[count];  // Is this right ??
            for (int i = 0; i < count; i++) // fill with valueDef
                _binary[i] = bdef;
        }

        public void SetBit(int bitPos, bool bitValue = true)
        {
            // Set state of a single bit.
            // bitPos = bit position in the _binary - kFirstBit
            // NOTE: This does NOT grow except if _binary == null

            if (bitPos > 0)    // One base instead of Zero base  
            {
                bitPos -= kFirstBit;
            }

            int position = bitPos / kBitsPer;
            if (_binary == null)
            {
                SetBitMask(Math.Max(kDefaultSize, position + 1), true);
            }

            if (_binary == null || position < 0 || position >= _binary.Length)
            {
                // If bitPos length is greater then the max length throw exception.  
                // NO Auto grow.
                throw new ArgumentOutOfRangeException(nameof(bitPos), bitPos, "BitMask bitPos too large");
            }

            int bitshift = bitPos % kBitsPer;

            byte x = _binary[position];
            if (bitValue)
            {
                x |= (byte)(1 << bitshift);
            }
            else
            {
                x &= (byte)~(1 << bitshift);
            }
            _binary[position] = x;
        }

        public bool IsSet(int bitPos, bool defaultValue = false)
        {
            // bitPos = bit position in the _binary
            if (_binary == null)
                return false;    // special
            if (bitPos <= 0)    // One base 
                return false;
            bitPos -= kFirstBit;

            int position = bitPos / kBitsPer;
            if (position < 0 || position >= _binary.Length)
            {
                // If bitPos length is greater then the token length just return defaultValue.
                return defaultValue;
            }

            int bitshift = bitPos % kBitsPer;

            // Get the byte from the byte Array that holds the bit
            // Move the bit to the LeastSignificantPosition (First Position) in the container
            int result = _binary[position] >> bitshift;

            // Check whether the bit value is 1 or 0
            // If the value is 0 return false //User is not authorized to use the module
            //  else if the value is 1 return true. //User has rights to use the module
            return (result & 1) != 0; // to bool
        }

        public static byte[] TruncateZeros(byte[] b)
        {
            // Truncate 0 off the end. No need to encode that.
            int len = b.Length;
            for (; len > 0 && b[len - 1] == 0; len--)
            {
            }
            if (len <= 0)
                return null;
            if (len == b.Length)
                return b;
            var b2 = new byte[len];
            Buffer.BlockCopy(b, 0, b2, 0, len);
            return b2;
        }

        public override string ToString()
        {
            // Get Base64 string.
            if (_binary == null)
                return null;
            var b = TruncateZeros(_binary);
            if (b == null)
                return "";
            return Convert.ToBase64String(b);
        }

        public static string CreateBitMask(int maxBits = kDefaultSize, bool defaultBitValue = false)
        {
            // Creates a new bitmask with all bits set to defaultBitValue.
            var t = new BitMask(maxBits, defaultBitValue);
            return t.ToString();
        }

        public static string CreateBitMask2(int bitPos, bool bitValue, string base64String)
        {
            // Create a new bitmask with one bit changed.
            // bitPos = bit position in the base64String to set.
            var t = new BitMask(base64String);
            t.SetBit(bitPos, bitValue);
            return t.ToString();
        }

        public static bool IsBitSet(int bitPos, string base64String)
        {
            // is bit set?
            // bitPos = bit position in the base64String
            var t = new BitMask(base64String);
            return t.IsSet(bitPos);
        }

        public void OpOr(BitMask bits)
        {
            // Combine these bits. Or bits.
            if (bits._binary == null)
                return;
            int bytes1 = bits._binary.Length;
            if (bytes1 <= 0)
                return;
            int bytes2 = (_binary == null) ? 0 : _binary.Length;
            if (bytes2 < bytes1)
            {
                Array.Resize(ref _binary, bytes1);
            }
            for (int i = 0; i < bytes1; i++)
            {
                _binary[i] |= bits._binary[i];
            }
        }

        public void OpAndNot(BitMask bits)
        {
            // Turn off bits that are indicated.
            // TODO
        }
    }
}

