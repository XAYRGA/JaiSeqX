using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.XAudio2;
using SharpDX.Multimedia;
using SharpDX.XAPO.Fx;
using System.IO;

namespace JaiSeqXLJA.DSP
{
    public class JAIDSPSoundBuffer
    {
        public WaveFormat format;
        public AudioBuffer buffer;
    }
}
