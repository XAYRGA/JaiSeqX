using Be.IO.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Be.IO
{
    public unsafe class BeBinaryReader : BinaryReader
    {
        private static readonly Encoding UTF8NoBom = new UTF8Encoding();

        protected readonly byte[] buffer;

        public BeBinaryReader(Stream s)
            : this(s, UTF8NoBom)
        { }

        public BeBinaryReader(Stream s, Encoding e)
            : this(s, e, false)
        { }

        public BeBinaryReader(Stream s, Encoding e, bool leaveOpen)
            : base(s, e, leaveOpen)
        {
            // Mirror code from BinaryReader.cs
            int bufferSize = e.GetMaxByteCount(1);
            if (bufferSize < 16)
                bufferSize = 16;
            this.buffer = new byte[bufferSize];
        }

        public override decimal ReadDecimal()
        {
            FillBuffer(16);
            fixed (byte* p = buffer)
                return BigEndian.ReadDecimal(p);
        }

        public override double ReadDouble()
        {
            FillBuffer(8);
            fixed (byte* p = buffer)
                return Reinterpret.Int64AsDouble(BigEndian.ReadInt64(p));
        }

        public override short ReadInt16()
        {
            FillBuffer(2);
            fixed (byte* p = buffer)
                return BigEndian.ReadInt16(p);
        }

        public override int ReadInt32()
        {
            FillBuffer(4);
            fixed (byte* p = buffer)
                return BigEndian.ReadInt32(p);
        }

        public override long ReadInt64()
        {
            FillBuffer(8);
            fixed (byte* p = buffer)
                return BigEndian.ReadInt64(p);
        }

        public override float ReadSingle()
        {
            FillBuffer(4);
            fixed (byte* p = buffer)
                return Reinterpret.Int32AsFloat(BigEndian.ReadInt32(p));
        }

        public override ushort ReadUInt16()
        {
            FillBuffer(2);
            fixed (byte* p = buffer)
                return (ushort)BigEndian.ReadInt16(p);
        }

        public override uint ReadUInt32()
        {
            FillBuffer(4);
            fixed (byte* p = buffer)
                return (uint)BigEndian.ReadInt32(p);
        }

        public override ulong ReadUInt64()
        {
            FillBuffer(8);
            fixed (byte* p = buffer)
                return (ulong)BigEndian.ReadInt64(p);
        }

        public uint ReadU24()
        {
            return (((uint)base.ReadByte()) << 16) | (((uint)base.ReadByte() << 8) | ((uint)base.ReadByte()));
        }
        protected override void FillBuffer(int numBytes)
        {
            if ((uint)numBytes > buffer.Length)
                Error.Range(nameof(numBytes), "Expected a non-negative value.");
            var s = BaseStream;
            if (s == null)
                Error.Disposed();
            int n, read = 0;
            do
            {
                n = s.Read(buffer, read, numBytes - read);
                if (n == 0)
                    Error.EndOfStream();
                read += n;
            } while (read < numBytes);
        }
    }
}
