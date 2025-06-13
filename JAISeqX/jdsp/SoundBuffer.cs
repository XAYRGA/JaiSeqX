using jaudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xayrga.bananapeel;
using System.Runtime.InteropServices;

namespace JAISeqX.jdsp
{


    internal unsafe static class SoundBufferHelper
    {
        private static byte[] wavhead = new byte[44] {
                        0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00,  0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74, 0x20,
                        0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00,  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x02, 0x00, 0x10, 0x00, 0x64, 0x61, 0x74, 0x61,  0x00, 0x00, 0x00, 0x00
        };

        public static IntPtr getWavePointer(short[] Samples, int rate)
        {
            var TempStream = new MemoryStream();
            var beW = new BinaryWriter(TempStream);
            var osz = Samples.Length * 2;
            var oszt = osz + 8;
            TempStream.Write(wavhead, 0, wavhead.Length);
            beW.BaseStream.Position = 4;
            beW.Write(oszt);
            beW.BaseStream.Position = 24;
            beW.Write(rate);
            beW.Write(rate);
            beW.BaseStream.Position = 40;
            beW.Write((int)osz);
            beW.Write(mux.PCM16ShortToByte(Samples));
            beW.Flush();
            var fileBuffer = TempStream.ToArray();
            beW.Close();
            TempStream.Close();
            var globalFileBuffer = Marshal.AllocHGlobal(fileBuffer.Length);
            Marshal.Copy(fileBuffer, 0, globalFileBuffer, fileBuffer.Length);
            return globalFileBuffer;
        }

    }

    internal unsafe class SoundBuffer
    {
        public LoopDescriptor Loop = new LoopDescriptor();
        public SoundBufferFormat Format = SoundBufferFormat.ADPCM4;
        public byte[] Buffer;
        public byte Channels = 0;
        public int SampleRate = 0;
        public int SampleCount = 0;
        public IntPtr BufferHandle;

        public SoundBuffer(byte[] Buffer, byte Channels, int SampleRate, SoundBufferFormat Format, bool Loop = false, int LoopStart = 0, int LoopEnd = 0)
        {
            this.Buffer = Buffer;
            this.Channels = Channels;
            this.Loop.Enable = Loop;
            this.Loop.Start = LoopStart;
            this.Loop.End = LoopEnd;
            this.SampleRate = SampleRate;
            this.Format = Format;
            this.SampleCount = Buffer.Length / 2;
        }

        public SoundBuffer(JWaveSystem.Wave Wave, byte[] buffer)
        {
            this.Buffer = buffer;
            Loop.Enable = Wave.Loop;
            Loop.Start = Wave.LoopStart;
            Loop.End = Wave.LoopEnd;
            SampleCount = Wave.SampleCount;
            Channels = 1;
            Format = (SoundBufferFormat)Wave.Format;
        }


        public unsafe IntPtr EncodeBuffer()
        {
            if (BufferHandle != IntPtr.Zero)
                return BufferHandle;

            short[] Samples;
            switch (Format)
            {
                case SoundBufferFormat.ADPCM4:
                    Samples = mux.ADPCM4TOPCM16(Buffer);
                    break;
                case SoundBufferFormat.ADPCM2:
                    Samples = mux.ADPCM2TOPCM16(Buffer);
                    break;
                case SoundBufferFormat.PCM8:
                    Samples = mux.PCM8216(Buffer);
                    break;
                case SoundBufferFormat.PCM16:
                    Samples = mux.PCM16ByteToShort(Buffer);
                    break;
                default:
                    Samples = new short[0];
                    break;
            }
            Buffer = new byte[0];
            return SoundBufferHelper.getWavePointer(Samples, SampleRate);
        }

        public class LoopDescriptor
        {
            public bool Enable = false;
            public int Start = 0;
            public int End = 0; 
        }      

        public enum SoundBufferFormat
        {
            ADPCM4 = 0,
            ADPCM2 = 1,
            PCM8 = 2,
            PCM16 = 3
        }
    }
}
