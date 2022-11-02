using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;

namespace jaudio
{

    public class InstrumentBank
    {
        public int globalID;
        public JInstrument[] Instruments;
        public Dictionary<int, JOscillator> Oscillators;
        public Dictionary<int, JEnvelope> Envelopes;
        public Dictionary<int, JInstrumentRandomEffect> RandomEffects;
        public Dictionary<int, JInstrumentSenseEffect> SenseEffects;
    }

    public class JVelocityRegion
    {
        public int mBaseAddress = 0;
        public byte Velocity;
        public ushort WSYSID;
        public ushort WaveID;
        public float Volume;
        public float Pitch;

    }

    public class JKeyRegion
    {
        public int mBaseAddress = 0;
        public byte BaseKey;
        public JVelocityRegion[] Velocities;

        public float Volume = 1f;
        public float Pitch = 1f;
        public float Unknown = 1f;


        public virtual JVelocityRegion getVelocity(int vel)
        {
            JVelocityRegion vReg = Velocities[0];
            for (int i = 0; i < Velocities.Length; i++)
                if (Velocities[i].Velocity >= vel)
                    return Velocities[i];
            return vReg;
        }
    }

    public class JEnvelope
    {

        public int mBaseAddress = 0;
        public int mTableOffset = 0;

        public uint mHash = 0;

        public JEnvelopeVector[] points;

        public class JEnvelopeVector
        {
            public ushort Mode;
            public ushort Delay;
            public short Value;
        }
    }

    public class JOscillator
    {
        public int mBaseAddress = 0;
        public uint mHash = 0;
        public byte Target;
        public float Rate;
        public JEnvelope Attack;
        public JEnvelope Release;
        public float Width;
        public float Vertex;
    }

    public class JInstrumentSenseEffect
    {
        public int mBaseAddress = 0;

        public byte Target;
        public byte Register;
        public byte Key;
        public float Floor;
        public float Ceiling;
    }
    public class JInstrumentRandomEffect
    {
        public int mBaseAddress = 0;
        public byte Target;
        public float Floor;
        public float Ceiling;
    }

    public class JInstrument
    {
        public int mBaseAddress = 0;

        public float Pitch = 1;
        public float Volume = 1;
        // These have to be initialized, since they're not initialized with a predetermiend size in v1

        public List<JOscillator> Oscillators = new List<JOscillator> ();
        public List<JInstrumentSenseEffect> SenseEffects = new List<JInstrumentSenseEffect>();
        public List <JInstrumentRandomEffect> RandomEffects = new List<JInstrumentRandomEffect> ();
        public List <JEnvelope> Envelopes = new List<JEnvelope> ();

        public JKeyRegion[] Keys;

        internal const int INST = 0x494E5354;
        internal const int PER2 = 0x50455232;
        public bool Percussion = false;

        public static JInstrument CreateFromStream(BeBinaryReader reader, int seekbase)
        {
            var magic = reader.ReadUInt32();
            if (magic == INST)
                return JStandardInstrumentv1.CreateFromStream(reader, seekbase);
            else if (magic == PER2)
                return JPercussion.CreateFromStream(reader, seekbase);
            return null;
        }

        public virtual JKeyRegion getKey(int key)
        {
            JKeyRegion kReg = Keys[0];
            for (int i = 0; i < Keys.Length; i++)
                if (Keys[i].BaseKey >= key)
                    return Keys[i];
            return kReg;
        }
    }

}
