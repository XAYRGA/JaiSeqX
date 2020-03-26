using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libJAudio
{
    public class JOscillator
    {
        public JOscillatorTarget target;
        public float rate;
        public float Width;
        public float Vertex;

        public JEnvelope[] envelopes = new JEnvelope[2];
    }

    public enum JOscillatorTarget
    {
        Volume = 1,
        Pitch = 2,
        Pan = 3,
        FX = 4,
        Dolby = 5
    }
}
