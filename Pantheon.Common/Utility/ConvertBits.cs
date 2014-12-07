using System;

namespace Pantheon.Common.Utility
{
    public static class ConvertBits
    {
        public static byte ToByte(sbyte value)
        {
            return unchecked((byte)value);
        }

        public static short ToInt16(ushort value)
        {
            byte[] data = BitConverter.GetBytes(value);
            return BitConverter.ToInt16(data, 0);
        }

        public static int ToInt32(uint value)
        {
            byte[] data = BitConverter.GetBytes(value);
            return BitConverter.ToInt32(data, 0);
        }

        public static long ToInt64(ulong value)
        {
            byte[] data = BitConverter.GetBytes(value);
            return BitConverter.ToInt64(data, 0);
        }

        public static sbyte ToSByte(byte value)
        {
            return unchecked((sbyte)value);
        }

        public static ushort ToUInt16(short value)
        {
            byte[] data = BitConverter.GetBytes(value);
            return BitConverter.ToUInt16(data, 0);
        }

        public static uint ToUInt32(int value)
        {
            byte[] data = BitConverter.GetBytes(value);
            return BitConverter.ToUInt32(data, 0);
        }

        public static ulong ToUInt32(long value)
        {
            byte[] data = BitConverter.GetBytes(value);
            return BitConverter.ToUInt64(data, 0);
        }
    }
}