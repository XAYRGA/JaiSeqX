using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Un4seen.Bass;
using System.Runtime;
using System.Runtime.InteropServices;

namespace JaiSeqX.Player.BassBuff
{


    public class SoundEffect : IDisposable
    {
        byte[] wdata;
        IntPtr mDataHandle;
        public bool loop;
        public int loopstart;
        public int loopend;
        public string dwave;
        public SoundEffect(string wav)
        {
            wdata = File.ReadAllBytes(wav);
            mDataHandle = Marshal.AllocHGlobal(wdata.Length);
            Marshal.Copy(wdata, 0, mDataHandle, wdata.Length);
            dwave = wav;
        }

        public SoundEffect(string wav, bool lo, int loops, int loope)
        {
            wdata = File.ReadAllBytes(wav);
            mDataHandle = Marshal.AllocHGlobal(wdata.Length);
            Marshal.Copy(wdata, 0, mDataHandle, wdata.Length);
            loop = lo;
            loopstart = loops;
            loopend = loope;
            dwave = wav;

        }

        public SoundEffectInstance CreateInstance()
        {
            var stream = Bass.BASS_StreamCreateFile(dwave, 0, 0, BASSFlag.BASS_DEFAULT);
            

            return new SoundEffectInstance(stream, loop, loopstart, loopend);

        }


        public void Dispose()
        {
            Marshal.FreeHGlobal(mDataHandle);
        }
    }
}
