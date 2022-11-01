using Be.IO.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Be.IO
{
    public unsafe class BeBinaryWriter : BinaryWriter
    {
        private static readonly Encoding UTF8NoBomThrows = new UTF8Encoding(false, true);

        protected readonly byte[] buffer;

        public BeBinaryWriter(Stream s)
            : this(s, UTF8NoBomThrows)
        { }

        public BeBinaryWriter(Stream s, Encoding e)
            : this(s, e, false)
        { }

        public BeBinaryWriter(Stream s, Encoding e, bool leaveOpen)
            : base(s, e, leaveOpen)
        {
            this.buffer = new byte[16];
        }

        public override void Write(decimal value)
        {
            fixed (byte* p = buffer)
                BigEndian.WriteDecimal(p, value);
            OutStream.Write(buffer, 0, 16);
        }

        public override void Write(double value)
        {
            fixed (byte* p = buffer)
                BigEndian.WriteInt64(p, Reinterpret.DoubleAsInt64(value));
            OutStream.Write(buffer, 0, 8);
        }

        public override void Write(float value)
        {
            fixed (byte* p = buffer)
                BigEndian.WriteDecimal(p, Reinterpret.FloatAsInt32(value));
            OutStream.Write(buffer, 0, 4);
        }

        public override void Write(int value)
        {
            fixed (byte* p = buffer)
                BigEndian.WriteInt32(p, value);
            OutStream.Write(buffer, 0, 4);
        }

        public override void Write(long value)
        {
            fixed (byte* p = buffer)
                BigEndian.WriteInt64(p, value);
            OutStream.Write(buffer, 0, 8);
        }

        public override void Write(short value)
        {
            fixed (byte* p = buffer)
                BigEndian.WriteInt16(p, value);
            OutStream.Write(buffer, 0, 2);
        }

        public override void Write(uint value)
        {
            fixed (byte* p = buffer)
                BigEndian.WriteInt32(p, (int)value);
            OutStream.Write(buffer, 0, 4);
        }

        public override void Write(ulong value)
        {
            fixed (byte* p = buffer)
                BigEndian.WriteInt64(p, (long)value);
            OutStream.Write(buffer, 0, 8);
        }

        public override void Write(ushort value)
        {
            fixed (byte* p = buffer)
                BigEndian.WriteInt16(p, (short)value);
            OutStream.Write(buffer, 0, 2);
        }

        public void WriteU24(int value)
        {
            Write((byte)((value >> 16) & 0xFF));
            Write((byte)((value >> 8) & 0xFF));
            Write((byte)(value & 0xFF));
        }

    }
}
