using System;
using System.Collections.Generic;
using System.Text;

namespace DotStd
{
    public static class ByteUtil
    {
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

        public static int ToIntLE(byte[] b, int offset)
        {
            // Convert 4 bytes to a 32 bit signed int. (Host Order) LittleEndian (Intel)  
            // Like BitConverter.ToInt32()
            if (b == null || b.Length < offset + 4)
                return 0;
            return (int)b[offset + 0] | ((int)b[offset + 1] << 8) | ((int)b[offset + 2] << 16) | ((int)b[offset + 3] << 24);
        }
        public static uint ToUIntLE(byte[] b, int offset)
        {
            // Convert 4 bytes to a 32 bit UNsigned int. (Host Order) LittleEndian (Intel)
            // Like BitConverter.ToUInt32()
            if (b == null || b.Length < offset + 4)
                return 0;
            return (uint)b[offset + 0] | ((uint)b[offset + 1] << 8) | ((uint)b[offset + 2] << 16) | ((uint)b[offset + 3] << 24);
        }

        public static ushort ToUShortLE(byte[] b, int offset)
        {
            // Convert 4 bytes to a 16 bit UNsigned short. (Host Order) LittleEndian (Intel)
            // Like BitConverter.ToUInt16()
            if (b == null || b.Length < offset + 2)
                return 0;
            return (ushort)(b[offset + 0] | (((ushort)b[offset + 1]) << 8));
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
