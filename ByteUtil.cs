using System;
using System.Collections.Generic;
using System.Text;

namespace DotStd
{
    public static class ByteUtil
    {
        public static bool IsNullOrZero(this byte[] val)
        {
            // similar to string.IsNullOrEmpty()
            if (val == null)
                return true;
            foreach (var b in val)
            {
                if (b != 0)
                    return false;
            }
            return true;
        }

        public static byte[] TruncateZeros(this byte[] b)
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

#if true
        public static int CompareBytes(this byte[] a1, byte[] a2)
        {
            // compare 2 arrays of bytes.
            // 0 = same.

            if (a1.Length != a2.Length)
                return a1.Length - a2.Length;

            for (int i = 0; i < a1.Length; i++)
                if (a1[i] != a2[i])
                    return a1[i] - a2[i];

            return 0;
        }
#else
        public static unsafe int CompareBytes(byte[] a1, byte[] a2)
        {
            // ASSUME a1 != a2 and both are not null.
            fixed (byte* p1 = a1, p2 = a2)
            {
                byte* x1 = p1;
                byte* x2 = p2;
                int l = Math.Min(a1.Length, a2.Length);

                if (l >= 8)
                {
                    int l8 = l / 8;
                    for (int i = 0; i < l8; i++, x1 += 8, x2 += 8)
                        if (*((long*)x1) != *((long*)x2))
                            return (int)(x1 - p1);
                }

                // remainder.
                if ((l & 4) != 0)
                {
                    if (*((int*)x1) != *((int*)x2))
                        return (int)(x1 - p1);
                    x1 += 4; x2 += 4;
                }
                if ((l & 2) != 0)
                {
                    if (*((short*)x1) != *((short*)x2))
                        return (int)(x1 - p1);
                    x1 += 2; x2 += 2;
                }
                if ((l & 1) != 0)
                {
                    if (*((byte*)x1) != *((byte*)x2))
                        return (int)(x1 - p1);
                }
                return l;   // full match to min length of both.
            }
        }
#endif

        public static byte[] GetChunk(this byte[] val, int offset, int size)
        {
            // Get some chunk of a byte array. Used in place of Skip/Take
            if (val == null)
                return null;
            size = Math.Min(val.Length - offset, size);  // Don't overrun the end.
            byte[] newBytes = new byte[size];
            Array.Copy(val, offset, newBytes, 0, size);
            return newBytes;
        }

        // like BitConverter but guaranteed to be LittleEndian.

        public static ushort ToUShortLE(byte[] b, int offset = 0)
        {
            // Convert 4 bytes to a 16 bit UNsigned short. (Host Order) LittleEndian (Intel)
            // Like BitConverter.ToUInt16()
            if (b == null)
            {
                return 0;
            }
            if (b.Length < offset + 2)
            {
                if (b.Length < offset + 1)
                    return 0;
                return b[offset];
            }

            return (ushort)(b[offset + 0] | (((ushort)b[offset + 1]) << 8));
        }

        public static int ToIntLE(byte[] b, int offset = 0)
        {
            // Convert 4 bytes to a 32 bit signed int. (Host Order) LittleEndian (Intel)  
            // Like BitConverter.ToInt32()

            if (b == null)
                return 0;
            if (b.Length < offset + 4)
                return ToUShortLE(b,offset);

            return (int)b[offset + 0] | ((int)b[offset + 1] << 8) | ((int)b[offset + 2] << 16) | ((int)b[offset + 3] << 24);
        }

        public static uint ToUIntLE(byte[] b, int offset = 0)
        {
            // Convert 4 bytes to a 32 bit UNsigned int. (Host Order) LittleEndian (Intel)
            // Like BitConverter.ToUInt32()
            if (b == null)
                return 0;
            if (b.Length < offset + 4)
                return ToUShortLE(b, offset);
            return (uint)b[offset + 0] | ((uint)b[offset + 1] << 8) | ((uint)b[offset + 2] << 16) | ((uint)b[offset + 3] << 24);
        }
        
        public static ulong ToULongLE(byte[] b, int offset=0)
        {
            if (b == null)
                return 0;
            uint valLow = ToUIntLE(b, offset);
            if (b.Length < offset + 8)
                return valLow;
            return valLow | ((ulong)b[offset + 4] << 32) | ((uint)b[offset + 5] << 40) | ((uint)b[offset + 6] << 48) | ((uint)b[offset + 7] << 56);
        }

        public static void PackIntLE(byte[] b, int offset, int value)
        {
            b[offset + 0] = (byte)(value);
            b[offset + 1] = (byte)(value >> 8);
            b[offset + 2] = (byte)(value >> 16);
            b[offset + 3] = (byte)(value >> 24);
        }
        public static void PackUIntLE(byte[] b, int offset, uint value)
        {
            b[offset + 0] = (byte)(value);
            b[offset + 1] = (byte)(value >> 8);
            b[offset + 2] = (byte)(value >> 16);
            b[offset + 3] = (byte)(value >> 24);
        }
        public static void PackUShortLE(byte[] b, int offset, ushort value)
        {
            b[offset + 0] = (byte)(value);
            b[offset + 1] = (byte)(value >> 8);
        }

    }
}
