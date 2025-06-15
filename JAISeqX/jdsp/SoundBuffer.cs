using jaudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xayrga.bananapeel;
using System.Runtime.InteropServices;

namespace jdsp
{


    internal unsafe static class SoundBufferHelper
    {        
        /* Oh... this is nasty :( */
        public static (IntPtr, int) getWavePointer(short[] Samples, int rate)
        {

            var TempStream = new MemoryStream();
            var BW = new BinaryWriter(TempStream);
            var wav = new PCM16WAV() {         
                format = 1,
                sampleRate = rate,
                channels = 1,
                bitsPerSample = 16,
                blockAlign = 2,
                buffer = Samples
            };
            wav.writeStreamLazy(BW);
    
            BW.Flush();
            TempStream.Flush();
            var fileBuffer = TempStream.ToArray();
            var size = TempStream.Length;
            BW.Close();
            TempStream.Close();
            var globalFileBuffer = Marshal.AllocHGlobal(fileBuffer.Length);
            Marshal.Copy(fileBuffer, 0, globalFileBuffer, fileBuffer.Length);
            return (globalFileBuffer, (int)size);
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
        public int HandleSize;

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
            SampleRate = (int)Wave.SampleRate;
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
            var handleResult = SoundBufferHelper.getWavePointer(Samples, SampleRate);
            HandleSize = handleResult.Item2;
            BufferHandle = handleResult.Item1;
            return handleResult.Item1;
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
