using System;
using System.Collections.Generic;
using System.Text;

namespace Wlpdf.Util
{
    public static class ByteConversions
    {
        public static int ReadBigEndianInt(byte [] buf, int start, int len)
        {
            int value = 0;
            for (int i = start; i < start + len; i++)
                value = (value << 8) | buf[i];
            return value;
        }

        public static void WriteBigEndianInt(int value, byte [] buf, int start, int len)
        {
            for(int i = start + len - 1; i >= start; i--)
            {
                buf[i] = (byte)(value & 255);
                value = value >> 8;
            }
        }
    }
}
