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
      
        public bool loop; // Do we loop
        public int loopstart; // Where do we start 
        public int loopend; // Where do we end
        public string dwave; // Wav PCM path
        public SoundEffect(string wav)
        {
            dwave = wav; // store pcm path
        }

        public SoundEffect(string wav, bool lo, int loops, int loope)
        {
         
            // See comments above for info.

            loop = lo;
            loopstart = loops;
            loopend = loope;
            dwave = wav;

        }

        public SoundEffectInstance CreateInstance()
        {
            var stream = Bass.BASS_StreamCreateFile(dwave, 0, 0, BASSFlag.BASS_DEFAULT); // Allocate the new stream
            return new SoundEffectInstance(stream, loop, loopstart, loopend); // Pass our instance parameters to the sound. 

        }


        public void Dispose()
        {
         
        }
    }
}
