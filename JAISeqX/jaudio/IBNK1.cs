using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Be.IO;


namespace jaudio
{
    public class JInstrumentBankv1 : InstrumentBank
    {

        public int mBaseAddress = 0;
 
        public uint mHash = 0;

        private const int IBNK = 0x49424e4b;
        private const int BANK = 0x42414E4B;

        public uint size = 0;
 
        public JInstrumentBankv1()
        {
            Instruments = new JInstrument[0xF0];
        }

        public void loadFromStream(BeBinaryReader reader)
        {
            int mountpos = (int)reader.BaseStream.Position;
            if (reader.ReadUInt32() != IBNK)
                throw new InvalidDataException("Data is not IBNK!");
            size = reader.ReadUInt32();
            globalID = reader.ReadInt32();
            reader.BaseStream.Position = mountpos + 0x20;
            if (reader.ReadUInt32() != BANK)
                throw new InvalidDataException("Data is not BANK");
            var instPtrs = util.readInt32Array(reader, 0xF0);
            for (int i = 0; i < 0xF0; i++)
            {
                reader.BaseStream.Position = instPtrs[i] + mountpos;
                if (instPtrs[i] != 0)
                    Instruments[i] = JInstrument.CreateFromStream(reader, mountpos);
            }
            dereferenceObjectTables();
        }

        private void dereferenceObjectTables() // Removes duplicate objects and fills tables.
        {

            var oscDedupe = new Dictionary<int, JOscillator>();
            var envDedupe = new Dictionary<int, JEnvelope>();
            var randDedupe = new Dictionary<int, JInstrumentRandomEffect>();
            var sensDedupe = new Dictionary<int, JInstrumentSenseEffect>();

            for (int i = 0; i < Instruments.Length; i++)
            {
                var cInst = Instruments[i];
                if (cInst == null || cInst.Percussion == true)
                    continue;

                var ins = (JStandardInstrumentv1)cInst;

                for (int o = 0; o < ins.Oscillators.Count; o++)
                    if (ins.Oscillators[o] != null)
                        if (oscDedupe.ContainsKey(ins.Oscillators[o].mBaseAddress))
                            ins.Oscillators[o] = oscDedupe[ins.Oscillators[o].mBaseAddress];
                        else
                        {
                            var osc = ins.Oscillators[o];
                            oscDedupe[osc.mBaseAddress] = osc;

                            if (osc.Attack != null)
                                if (!envDedupe.ContainsKey(osc.Attack.mBaseAddress))
                                    envDedupe[osc.Attack.mBaseAddress] = osc.Attack;
                            if (osc.Release != null)
                                if (!envDedupe.ContainsKey(osc.Release.mBaseAddress))
                                    envDedupe[osc.Release.mBaseAddress] = osc.Release;
                        }

                for (int o = 0; o < ins.RandomEffects.Count; o++)
                    if (ins.RandomEffects[o] != null)
                        if (randDedupe.ContainsKey(ins.RandomEffects[o].mBaseAddress))
                            ins.RandomEffects[o] = randDedupe[ins.RandomEffects[o].mBaseAddress];
                        else
                            randDedupe[ins.RandomEffects[o].mBaseAddress] = ins.RandomEffects[o];

                for (int o = 0; o < ins.SenseEffects.Count; o++)
                    if (ins.SenseEffects[o] != null)
                        if (randDedupe.ContainsKey(ins.SenseEffects[o].mBaseAddress))
                            ins.SenseEffects[o] = sensDedupe[ins.SenseEffects[o].mBaseAddress];
                        else
                            sensDedupe[ins.SenseEffects[o].mBaseAddress] = ins.SenseEffects[o];

            }
            foreach (KeyValuePair<int, JOscillator> b in oscDedupe)
                Oscillators[b.Key] = b.Value;

            foreach (KeyValuePair<int, JInstrumentSenseEffect> b in sensDedupe)
                SenseEffects[b.Key] = b.Value;

            foreach (KeyValuePair<int, JInstrumentRandomEffect> b in randDedupe)
                RandomEffects[b.Key] = b.Value;

            foreach (KeyValuePair<int, JEnvelope> b in envDedupe)
                Envelopes[b.Key] = b.Value;
        }

        public static JInstrumentBankv1 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentBankv1();
            b.loadFromStream(reader);
            return b;
        }

        public void WriteToStream(BeBinaryWriter wr)
        {
            wr.Write(IBNK);
            wr.Write(size);
            wr.Write(globalID); wr.Flush();
            wr.Write(new byte[0x14]);
            wr.Write(BANK);
            for (int i = 0; i < Instruments.Length; i++)
                wr.Write(Instruments[i] == null ? 0 : Instruments[i].mBaseAddress);                
        }
    }

    public class JInstrumentEnvelopev1 : JEnvelope
    {

        private void loadFromStream(BeBinaryReader reader)
        {
            var origPos = reader.BaseStream.Position;
            mBaseAddress = (int)origPos;
            int count = 0;
            while (reader.ReadUInt16() < 0xB) {
                reader.ReadUInt32();
                count++;
            }
            count++;
            reader.BaseStream.Position = origPos;
            points = new JEnvelopeVector[count];
            for (int i = 0; i < count; i++)
                points[i] = new JEnvelopeVector { Mode = reader.ReadUInt16(), Delay = reader.ReadUInt16(), Value = reader.ReadInt16() };
        }
        public static JInstrumentEnvelopev1 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentEnvelopev1();
            b.loadFromStream(reader);        
            return b;
        }
        public void WriteToStream(BeBinaryWriter wr)
        {
            mBaseAddress = (int)wr.BaseStream.Position;
            var remainingLength = 32;
            for (int i=0; i < points.Length;i++)
            {
                remainingLength -= 6;
                wr.Write(points[i].Mode);
                wr.Write(points[i].Delay);
                wr.Write(points[i].Value);
            }
            if (remainingLength > 0)
                wr.Write(new byte[remainingLength]);
            else
                util.padTo(wr, 32);

        }
    }

    public class JInstrumentOscillatorv1 : JOscillator
    {
   
        private void loadFromStream(BeBinaryReader reader, int seekbase)
        {
            mBaseAddress = (int)reader.BaseStream.Position;
            Target = reader.ReadByte();
            reader.ReadBytes(3);
            Rate = reader.ReadSingle();
            var envA = reader.ReadUInt32();
            var envB = reader.ReadUInt32();
            Width = reader.ReadSingle();
            Vertex = reader.ReadSingle();
            reader.BaseStream.Position = envA + seekbase;
            if (envA > 0)
                Attack = JInstrumentEnvelopev1.CreateFromStream(reader);
            reader.BaseStream.Position = envB + seekbase;
            if (envB > 0)
                Release = JInstrumentEnvelopev1.CreateFromStream(reader);
        }
        public static JInstrumentOscillatorv1 CreateFromStream(BeBinaryReader reader, int seekbase)
        {
            var b = new JInstrumentOscillatorv1();
            b.loadFromStream(reader,seekbase);
            return b;
        }

        public void WriteToStream(BeBinaryWriter wr)
        {
            mBaseAddress = (int)wr.BaseStream.Position;
            wr.Write(Target);
            wr.Write(new byte[0x3]);
            wr.Write(Rate);
            wr.Write(Attack.mBaseAddress);
            wr.Write(Release.mBaseAddress);
            wr.Write(Width);
            wr.Write(Vertex);
            wr.Write(new byte[8]);
        }
    }

    public class JInstrumentSenseEffectv1 : JInstrumentSenseEffect
    {
        
        private void loadFromStream(BeBinaryReader reader)
        {
            mBaseAddress = (int)reader.BaseStream.Position;
            Target = reader.ReadByte();
            Register = reader.ReadByte();
            Key = reader.ReadByte();
            reader.ReadBytes(1);
            Floor = reader.ReadSingle();
            Ceiling = reader.ReadSingle();
        }
        public static JInstrumentSenseEffectv1 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentSenseEffectv1();
            b.loadFromStream(reader);
            return b;
        }
    }

    public class JInstrumentRandEffectv1 : JInstrumentRandomEffect
    {
        private void loadFromStream(BeBinaryReader reader)
        {
            mBaseAddress= (int)reader.BaseStream.Position;
            Target = reader.ReadByte();
            reader.ReadBytes(3);
            Floor = reader.ReadSingle();
            Ceiling = reader.ReadSingle();
        }
        public static JInstrumentRandEffectv1 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentRandEffectv1();
            b.loadFromStream(reader);
            return b;
        }
    }


    public class JStandardInstrumentv1 : JInstrument
    {

        private void loadFromStream(BeBinaryReader reader, int seekbase)
        {
            mBaseAddress = (int)reader.BaseStream.Position;
            reader.ReadUInt32(); // Empty 
            Pitch = reader.ReadSingle();
            Volume = reader.ReadSingle();

            var oscA = reader.ReadUInt32();
            var oscB = reader.ReadUInt32();
            var effA = reader.ReadUInt32();
            var effB = reader.ReadUInt32();
            var ranA = reader.ReadUInt32();
            var ranB = reader.ReadUInt32();

            var keyRegCount = reader.ReadUInt32();
            var keyRegPtrs = util.readInt32Array(reader, (int)keyRegCount);
            Keys = new JKeyRegionv1[keyRegCount];

            reader.BaseStream.Position = oscA + seekbase;
            if (oscA > 0)
                Oscillators.Add(JInstrumentOscillatorv1.CreateFromStream(reader, seekbase));
            reader.BaseStream.Position = oscB + seekbase;
            if (oscB > 0)
                Oscillators.Add(JInstrumentOscillatorv1.CreateFromStream(reader, seekbase));


            reader.BaseStream.Position = effA + seekbase;
            if (effA > 0)
                 SenseEffects.Add(JInstrumentSenseEffectv1.CreateFromStream(reader));

            reader.BaseStream.Position = effB + seekbase;
            if (effB > 0)
                SenseEffects.Add(JInstrumentSenseEffectv1.CreateFromStream(reader));


            reader.BaseStream.Position = ranA + seekbase;
            if (ranA > 0)
                RandomEffects.Add(JInstrumentRandEffectv1.CreateFromStream(reader));

            reader.BaseStream.Position = ranB + seekbase;
            if (ranB > 0)
                RandomEffects.Add(JInstrumentRandEffectv1.CreateFromStream(reader));

            for (int i=0; i < keyRegCount; i++)
            {
                reader.BaseStream.Position = keyRegPtrs[i] + seekbase;
                if (keyRegPtrs[i] != 0)
                    Keys[i] = JKeyRegionv1.CreateFromStream(reader, seekbase);
            }
        }
        new public static JStandardInstrumentv1 CreateFromStream(BeBinaryReader reader, int seekbase)
        {
            var b = new JStandardInstrumentv1();
            b.loadFromStream(reader,seekbase);
            return b;
        }
    }

    public class JPercussion : JInstrument
    {
        public JPercussionEntry[] Sounds = new JPercussionEntry[100];
        private void loadFromStream(BeBinaryReader reader, int seekbase)
        {
            Percussion = true;
            reader.ReadBytes(0x84); // Padding. 
            var keyRegPtrs = util.readInt32Array(reader, 100);
            var anchor = reader.BaseStream.Position; // Store anchor at end of pointer table at base + 0x218
            for (int i=0; i < 100; i++)
                if (keyRegPtrs[i]!=0)
                {
                    reader.BaseStream.Position = keyRegPtrs[i] + seekbase;
                    Sounds[i] = JPercussionEntry.CreateFromStream(reader, seekbase);
                }
            reader.BaseStream.Position = anchor;  // Restore anchor, JPercussionEntry.CreateFromStream destroyed our position

            reader.ReadBytes(0x70); // Padding 
            for (int i = 0; i < 100; i++)
            {
                var b = reader.ReadByte();
                if (keyRegPtrs[i] != 0)
                    Sounds[i].uflag1 = b;
            }
            reader.ReadBytes(0x1c); // Also padding
            for (int i = 0; i < 100; i++)
            {
                var b = reader.ReadUInt16();
                if (keyRegPtrs[i] != 0)
                    Sounds[i].uflag2 = b;
            }
            // 0x50 padding.
            reader.ReadBytes(0x50);
        }

        new public static JPercussion CreateFromStream(BeBinaryReader reader, int seekbase)
        {
            var b = new JPercussion();
            b.loadFromStream(reader, seekbase);
            return b;
        }
    }


    public class JPercussionEntry  : JKeyRegion
    {
        
        public int mBaseAddress = 0;
        public byte uflag1;
        public ushort uflag2;


        private void loadFromStream(BeBinaryReader reader, int seekbase)
        {
            Pitch = reader.ReadSingle();
            Volume = reader.ReadSingle();
            reader.ReadBytes(8);
            var velregCount = reader.ReadInt32();
            Velocities = new JVelocityRegionv1[velregCount];
            var velRegPtrs = util.readInt32Array(reader, velregCount);
            for (int i = 0; i < velregCount; i++)
            {
                reader.BaseStream.Position = seekbase + velRegPtrs[i];
                Velocities[i] = JVelocityRegionv1.CreateFromStream(reader, seekbase);
            }
        }
       public static JPercussionEntry CreateFromStream(BeBinaryReader reader, int seekbase)
        {
            var b = new JPercussionEntry();
            b.loadFromStream(reader, seekbase);
            return b;
        }
    }

  
    public class JKeyRegionv1 : JKeyRegion
    {       
        private void loadFromStream(BeBinaryReader reader, int seekbase)
        {
            BaseKey = reader.ReadByte();
            reader.ReadBytes(3);
            var velregCount = reader.ReadInt32();
            Velocities = new JVelocityRegionv1[velregCount];
            var velRegPtrs = util.readInt32Array(reader, velregCount);
            for (int i = 0; i < velregCount; i++) {
                reader.BaseStream.Position = seekbase + velRegPtrs[i];
                Velocities[i] = JVelocityRegionv1.CreateFromStream(reader, seekbase);
            }
        }
        public static JKeyRegionv1 CreateFromStream(BeBinaryReader reader, int seekbase)
        {
            var b = new JKeyRegionv1();
            b.loadFromStream(reader, seekbase);
            return b;
        }
    }

    

    public class JVelocityRegionv1 : JVelocityRegion
    {
        
        private void loadFromStream(BeBinaryReader reader, int seekbase)
        {
            Velocity = reader.ReadByte();
            reader.ReadBytes(3); // empty 
            WSYSID = reader.ReadUInt16();
            WaveID = reader.ReadUInt16();
            Volume = reader.ReadSingle();
            Pitch = reader.ReadSingle();
        }
        public static JVelocityRegionv1 CreateFromStream(BeBinaryReader reader, int seekbase)
        {
            var b = new JVelocityRegionv1();
            b.loadFromStream(reader, seekbase);
            return b;
        }
    }
}
