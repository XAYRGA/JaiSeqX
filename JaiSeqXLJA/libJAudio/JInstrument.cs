using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Be.IO;
namespace libJAudio
{
    public class JInstrumentKey
    {
        public float Volume = 1;
        public float Pitch = 1;
        public int baseKey;
        public JInstrumentKeyVelocity[] Velocities; 
    }


    public class JInstrumentKeyVelocity
    {
        public int baseVel;
        public float Volume;
        public float Pitch;
        public int wave;
        public int wsysid;
        public int velocity;
    }


    public class JInstrument
    {
        public int id;
        public float Volume;
        public float Pitch;
        public byte oscillatorCount;
        public JOscillator[] oscillators;     
        public bool IsPercussion; 
        public JInstrumentKey[] Keys; 

    }
}
