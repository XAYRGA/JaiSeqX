using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xayrga.byteglider
{
    public unsafe class bgWriter : BinaryWriter
    {
        Stack<long> Anchors = new Stack<long>();
        long RelativeOffset = 0;
        
        Dictionary<string, long> Saves = new Dictionary<string, long>();

        protected readonly byte[] buffer = new byte[16];

        public bgWriter(Stream output) :base(output, new UTF8Encoding(false, true), false)
        {
        }

        public bgWriter(Stream output, Encoding encoding) : base(output, encoding, false)
        {
        }

        public bgWriter(Stream output, Encoding encoding, bool leaveOpen) : base(output, encoding, leaveOpen)
        {

        }


        public void WriteBE(byte value)
        {
            Write(value); // Alias it, keeps code consistent.
        }

        public void WriteBE(sbyte value)
        {
            Write(value); // Alias it, keeps code consistent.
        }

        public void WriteBE(short value)
        {
            var ip = &value;
            var bp = (byte*)ip;
            buffer[0] = bp[1];
            buffer[1] = bp[0];
            OutStream.Write(buffer, 0, 2);
        }

        public void WriteBE(ushort value)
        {
            var ip = &value;
            var bp = (byte*)ip;
            buffer[0] = bp[1];
            buffer[1] = bp[0];
            OutStream.Write(buffer, 0, 2);
        }

        public void WriteBE(int value)
        {
            var ip = &value;
            var bp = (byte*)ip;
            buffer[0] = bp[3];
            buffer[1] = bp[2];
            buffer[2] = bp[1];
            buffer[3] = bp[0];
            OutStream.Write(buffer, 0, 4);
        }

        public void WriteBE(uint value)
        {
            var ip = &value;
            var bp = (byte*)ip;
            buffer[0] = bp[3];
            buffer[1] = bp[2];
            buffer[2] = bp[1];
            buffer[3] = bp[0];
            OutStream.Write(buffer, 0, 4);
        }

        public void WriteBE(uint value, bool u24)
        {
            var ip = &value;
            var bp = (byte*)ip;
            buffer[0] = bp[2];
            buffer[1] = bp[1];
            buffer[2] = bp[0];

            OutStream.Write(buffer, 0, 3);
        }


        public void WriteBE(long value)
        {
            var ip = &value;
            var bp = (byte*)ip;
            buffer[0] = bp[7];
            buffer[1] = bp[6];
            buffer[2] = bp[5];
            buffer[3] = bp[4];
            buffer[4] = bp[3];
            buffer[5] = bp[2];
            buffer[6] = bp[1];
            buffer[7] = bp[0];
            OutStream.Write(buffer, 0, 8);
        }

        public void WriteBE(ulong value)
        {
            var ip = &value;
            var bp = (byte*)ip;
            buffer[0] = bp[7];
            buffer[1] = bp[6];
            buffer[2] = bp[5];
            buffer[3] = bp[4];
            buffer[4] = bp[3];
            buffer[5] = bp[2];
            buffer[6] = bp[1];
            buffer[7] = bp[0];
            OutStream.Write(buffer, 0, 8);
        }


        public void WriteBE(float value)
        {
            var ip = &value;
            var bp = (byte*)ip;
            buffer[0] = bp[3];
            buffer[1] = bp[2];
            buffer[2] = bp[1];
            buffer[3] = bp[0];
            OutStream.Write(buffer, 0, 4);
        }

        public void WriteBE(double value)
        {
            var bp = (byte*)(&value);
            buffer[0] = bp[7];
            buffer[1] = bp[6];
            buffer[2] = bp[5];
            buffer[3] = bp[4];
            buffer[4] = bp[3];
            buffer[5] = bp[2];
            buffer[6] = bp[1];
            buffer[7] = bp[0];
            OutStream.Write(buffer, 0, 8);
        }


        public void Write(string value,char terminator)
        {
            var wq = Encoding.ASCII.GetBytes(value);
            Write(wq); // Pass off work to underlying binary stream
            Write(terminator); // Little endian anyways. 
        }

        public void PushAnchor()
        {
            Anchors.Push(BaseStream.Position);
        }

        public void PopAnchor()
        {
            BaseStream.Position = Anchors.Pop();
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

        public void Seek(long offset, bool absolute = false)
        {
            BaseStream.Seek(offset + (absolute ? 0 : RelativeOffset), SeekOrigin.Begin);
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

        public void PadAlign(int alignment = 16, BGAlignDirection direction = BGAlignDirection.FORWARD, bool dontAdd = false)
        {
            var currentPos = BaseStream.Position;
            var remainder = currentPos % alignment;

            if (remainder == 0 && dontAdd == false)
                return;
            else if (direction == BGAlignDirection.FORWARD)
                Write(new byte[alignment - remainder]);
            else
                BaseStream.Position -= remainder;
        }

        public void FlushAndClose()
        {
            Flush();
            Close();
        }
    }
}
