using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Be.IO.Helpers
{
    internal unsafe static class BigEndian
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16(byte* p)
        {
            return (short)(p[0] << 8 | p[1]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32(byte* p)
        {
            return p[0] << 24 | p[1] << 16 | p[2] << 8 | p[3];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64(byte* p)
        {
            int lo = ReadInt32(p);
            int hi = ReadInt32(p + 4);
            return (long)hi << 32 | (uint)lo;
        }

        public static decimal ReadDecimal(byte* p)
        {
            decimal result;
            int* d = (int*)&result;
            int lo = ReadInt32(p);
            int mid = ReadInt32(p + 4);
            int hi = ReadInt32(p + 8);
            int flags = ReadInt32(p + 12);
            d[0] = flags;
            d[1] = hi;
            d[2] = lo;
            d[3] = mid;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16(byte* p, short s)
        {
            p[0] = (byte)(s >> 8);
            p[1] = (byte)s;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32(byte* p, int i)
        {
            p[0] = (byte)(i >> 24);
            p[1] = (byte)(i >> 16);
            p[2] = (byte)(i >> 8);
            p[3] = (byte)i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64(byte* p, long l)
        {
            p[0] = (byte)(l >> 56);
            p[1] = (byte)(l >> 48);
            p[2] = (byte)(l >> 40);
            p[3] = (byte)(l >> 32);
            p[4] = (byte)(l >> 24);
            p[5] = (byte)(l >> 16);
            p[6] = (byte)(l >> 8);
            p[7] = (byte)l;
        }

        public static void WriteDecimal(byte* p, decimal d)
        {
            int* i = (int*)&d;
            int flags = i[0];
            int hi = i[1];
            int lo = i[2];
            int mid = i[3];
            WriteInt32(p, lo);
            WriteInt32(p + 4, mid);
            WriteInt32(p + 8, hi);
            WriteInt32(p + 12, flags);
        }
    }
}
