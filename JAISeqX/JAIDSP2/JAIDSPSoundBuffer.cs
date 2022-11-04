using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime;
using System.IO;
using Un4seen.Bass;
using Be.IO;
using System.Runtime.InteropServices;

namespace JAISeqX.JAIDSP2
{
    public class JAIDSPSoundBuffer : IDisposable
    {
        public JAIDSPFormat format;
        public bool looped;
        public int loopStart;
        public int loopEnd;
        public byte[] buffer;
        public byte[] fileBuffer;
        public byte[] bufferHandle;
        public IntPtr globalFileBuffer;

        private static byte[] wavhead = new byte[44] {
                        0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00,  0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74, 0x20,
                        0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00,  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x02, 0x00, 0x10, 0x00, 0x64, 0x61, 0x74, 0x61,  0x00, 0x00, 0x00, 0x00
        };
        
        public void generateFileBuffer()
        {
            var MS = new MemoryStream();
            var beW = new BinaryWriter(MS);
            var osz = buffer.Length;
            var oszt = osz + 8;
            MS.Write(wavhead, 0, wavhead.Length);
            beW.BaseStream.Position = 4;
            beW.Write(oszt);
            beW.BaseStream.Position = 24;
            beW.Write((int)format.sampleRate);
            beW.Write((int)format.sampleRate);
            beW.BaseStream.Position = 40;
            beW.Write((int)osz);
            beW.Write(buffer, 0, buffer.Length);
            beW.Flush();
            fileBuffer = MS.ToArray();
            beW.Close();
            MS.Close();
            globalFileBuffer = Marshal.AllocHGlobal(fileBuffer.Length);
            Marshal.Copy(fileBuffer, 0, globalFileBuffer, fileBuffer.Length);
            //beW.BaseStream.Position = 0;
        }

        public void Dispose()
        {
            if (globalFileBuffer!=null)
                Marshal.FreeHGlobal(globalFileBuffer);
        }       
    }
}
