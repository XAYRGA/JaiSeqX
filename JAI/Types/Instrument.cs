using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Be.IO;
namespace JaiSeqX.JAI.Types
{
    public class InstrumentKey
    {
        public float Volume = 1;
        public float Pitch = 1;
        public InstrumentKeyVelocity[] keys; 

    }


    public class InstrumentKeyVelocity
    {
        public float Volume;
        public float Pitch;
        public uint wave;
        public uint wsysid;
        public uint velocity;

    }


    public class Instrument
    {
        public int id;
        public float Volume;
        public float Pitch;
        public short attack = 0;
        public short decay = 0;
        public short sustain = 0;
        public short release = 0;
        public int oscillator = 0;
        public bool IsPercussion; 
       
        public InstrumentKey[] Keys; 

    }
}
