using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xayrga.byteglider
{
    public unsafe class bgReader : BinaryReader
    {
        Stack<long> Anchors = new Stack<long>();
        Dictionary<string, long> Saves = new Dictionary<string, long>();

        protected readonly byte[] buffer = new byte[16];
        private long RelativeOffset = 0;

        public bgReader(Stream input) : base(input)
        {
        }

        public bgReader(Stream input, Encoding encoding) : base(input, encoding)
        {
        }

        public bgReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
        {
        }

        public int ReadInt32BE()
        {
            FillBuffer(4);
            fixed (byte* p = buffer)
                return p[0] << 24 | p[1] << 16 | p[2] << 8 | p[3];
        }


        public uint ReadUInt24BE()
        {
            FillBuffer(3);
            fixed (byte* p = buffer)
                return (uint)(p[0] << 16 | p[1] << 8 | p[2] );
        }


        public uint ReadUInt32BE()
        {
            FillBuffer(4);
            fixed (byte* p = buffer)
                return (uint)(p[0] << 24 | p[1] << 16 | p[2] << 8 | p[3]);
        }

        public long ReadInt64BE()
        {
            FillBuffer(8);
            fixed (byte* p = buffer)
            {
                int kL = p[4] << 24 | p[5] << 16 | p[6] << 8 | p[7];
                int kH = p[0] << 24 | p[1] << 16 | p[2] << 8 | p[3];
                return (long)kH << 32 | (uint)kL;
            }
        }

        public ulong ReadUInt64BE()
        {
            FillBuffer(8);
            fixed (byte* p = buffer)
            {
                int kL = p[4] << 24 | p[5] << 16 | p[6] << 8 | p[7];
                int kH = p[0] << 24 | p[1] << 16 | p[2] << 8 | p[3];
                return (ulong)kH << 32 | (uint)kL;
            }
        }

        public double ReadDoubleBE()
        {
            var flt = ReadInt64BE();
            var fltP = &flt;
            var dbl = (double*)fltP;
            return dbl[0];
        }

        public float ReadSingleBE()
        {
            var flt = ReadInt32BE();
            var fltP = &flt;
            var sgl = (float*)fltP;
            return sgl[0];
        }

        public short ReadInt16BE()
        {
            FillBuffer(2);
            fixed (byte* p = buffer)
                return (short)(p[0] << 8 | p[1]);
        }

        public ushort ReadUInt16BE()
        {
            FillBuffer(2);
            fixed (byte* p = buffer)
                return (ushort)(p[0] << 8 | p[1]);
        }

        public int[] ReadInt32Array(int count)
        {
            var ret = new int[count];
            for (int i = 0; i < count; i++)
                ret[i] = ReadInt32();
            return ret;
        }

        public int[] ReadInt32ArrayBE(int count)
        {
            var ret = new int[count];
            for (int i = 0; i < count; i++)
                ret[i] = ReadInt32BE();
            return ret;
        }

        public string ReadTerminatedString(char term = '\x00')
        {
            string ret = "";
            char val = (char)0;
            while ((val = ReadChar()) != term)
                ret += val;
            return ret;
        }

        public void FollowPointerAnchor32()
        {
            var ptr = ReadUInt32();
            PushAnchor();
            BaseStream.Position = ptr;
        }


        public void FollowPointerAnchor64()
        {
            var ptr = ReadInt64();
            PushAnchor();
            BaseStream.Position = ptr;
        }

        public void FollowPointerAnchor32BE()
        {
            var ptr = ReadUInt32BE();
            PushAnchor();
            BaseStream.Position = ptr;
        }

        public void FollowPointer32()
        {
            BaseStream.Position = ReadUInt32();
        }

        public void FollowPointer64()
        {
            BaseStream.Position = ReadInt64();
        }

        public void FollowPointer32BE()
        {
            BaseStream.Position = ReadUInt32BE();
        }

        public void FollowPointer64BE()
        {
            BaseStream.Position = ReadInt64BE();
        }

        public void PushAnchor()
        {
            Anchors.Push(BaseStream.Position);
        }

        public void PopAnchor()
        {
            var pop = Anchors.Pop();
            BaseStream.Position = pop;
        }

        public long PopAnchorNoMove()
        {
            return Anchors.Pop();
        }

        public void ClearAnchors()
        {
            Anchors.Clear();
        }

        public long PeekAnchor()
        {
            return Anchors.Peek();
        }

        public void SavePosition(string reference, int address)
        {
            Saves[reference] = address;
        }

        public void SavePosition(string reference)
        {
            Saves[reference] = BaseStream.Position;
        }

        public void GoPosition(string reference)
        {
            BaseStream.Position = Saves[reference];
        }
        public long GetSavedPosition(string reference)
        {
            return Saves[reference];
        }

        public void Flush()
        {
            BaseStream.Flush();
        }

        public void FlushAndClose()
        {
            BaseStream.Flush();
            BaseStream.Close();
        }

        public void Seek(long offset, bool absolute = false)
        {
            BaseStream.Seek(offset + (absolute ? 0 : RelativeOffset) , SeekOrigin.Begin);
        }

        public void Skip(int bytes)
        {
            BaseStream.Seek(bytes, SeekOrigin.Current);
        }

        public long GetPosition(bool absolute = false)
        {
            return BaseStream.Position + (absolute ? 0 : RelativeOffset);
        }

        public void SetBase(long relative = 0)
        {
            RelativeOffset = relative;
        }

        public void SetBaseCurrentPos()
        {
            RelativeOffset = BaseStream.Position;
        }

        public void ClearBase()
        {
            RelativeOffset = 0;
        }



        public void Align(int alignment = 16, BGAlignDirection direction = BGAlignDirection.FORWARD)
        {
            var currentPos = BaseStream.Position;
            var remainder = currentPos % alignment;

            if (remainder == 0)
                return;
            else if (direction == BGAlignDirection.FORWARD)
                BaseStream.Position += (alignment - remainder);
            else
                BaseStream.Position -= remainder;
        }

        protected override void FillBuffer(int numBytes)
        {
            if ((uint)numBytes > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(numBytes), "Expected a non-negative value.");

            var s = BaseStream;
            if (s == null)
                throw new ObjectDisposedException(this.GetType().Name);

            int n, read = 0;
            do
            {
                n = s.Read(buffer, read, numBytes - read);
                if (n == 0)
                    throw new EndOfStreamException();
                read += n;
            } while (read < numBytes);
        }

    }
}