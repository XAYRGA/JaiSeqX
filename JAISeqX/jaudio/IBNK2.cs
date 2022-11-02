using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;

namespace jaudio
{
    public class JInstrumentBankv2 : InstrumentBank
    {

        public int flags; 


        private const int PERC = 0x50455243; // Percussion Table
        private const int SENS = 0x53454E53; // Sensor effect
        private const int RAND = 0x52414E44; // Random Effect
        private const int OSCT = 0x4F534354; // Oscillator Table
        private const int INST = 0x494E5354; // INStrument Table
        private const int IBNK = 0x49424E4B; // Instrument BaNK
        private const int ENVT = 0x454E5654; // ENVelope Table
        private const int PMAP = 0x504D4150; // Percission Map Table
        private const int LIST = 0x4C495354; // Instrument / Percussion List
 

        uint Boundaries = 0;
        private int iBase = 0;
        private int OscTableOffset = 0;
        private int EnvTableOffset = 0;
        private int RanTableOffset = 0;
        private int SenTableOffset = 0;
        private int InsTableOffset = 0;
        private int ListTableOffset = 0;
        private int PercTableOffset = 0;
        private int PmapTableOffset = 0;

        public JStandardInstrumentv2[] Programs = new JStandardInstrumentv2[0];
        public JInstrumentPercussionMapv2[] PercussionMaps = new JInstrumentPercussionMapv2[0];
        public JPercussionInstrumentv2[] Percussions = new JPercussionInstrumentv2[0];
   


        private int findChunk(BeBinaryReader read, int chunkID, bool immediate = false)
        {
            if (!immediate) 
                read.BaseStream.Position = iBase;

            while (true)
            {
                var pos = (int)read.BaseStream.Position;
                var i = read.ReadInt32(); 
                if (i == chunkID) 
                    return pos; 
                else if (pos > (Boundaries))
                    return 0;
            }
        }

        private void loadFromStream(BeBinaryReader reader)
        {
            
            if (reader.ReadUInt32() != IBNK)
                throw new InvalidOperationException("Data is not IBNK");
            var ibnkSize = reader.ReadUInt32();
            Boundaries = ibnkSize;
            globalID = reader.ReadInt32();
            var flags = reader.ReadInt32();
            reader.ReadBytes(0x10);

            var origPos = reader.BaseStream.Position;
            EnvTableOffset = findChunk(reader, ENVT, true);
            OscTableOffset = findChunk(reader, OSCT, true);
            RanTableOffset = findChunk(reader, RAND, true);
            SenTableOffset = findChunk(reader, SENS, true);
            InsTableOffset = findChunk(reader, INST, true);
            PmapTableOffset = findChunk(reader, PMAP, true);
            PercTableOffset = findChunk(reader, PERC, true);
            ListTableOffset  = findChunk(reader, LIST, true);


            loadEnvelopes(reader, EnvTableOffset);
            loadOscillators(reader, OscTableOffset);
            loadRandEffs(reader, RanTableOffset);
            loadSensEffs(reader, SenTableOffset);
            loadInstruments(reader, InsTableOffset);
            loadPmaps(reader, PmapTableOffset);
            loadPercussion(reader, PercTableOffset);
            loadListMap(reader, ListTableOffset);
        }

        private void loadEnvelopes(BeBinaryReader rd, int envTableOffset)
        {
            rd.BaseStream.Position = envTableOffset;
            if (rd.ReadInt32() != ENVT)
                throw new Exception("Expected ENVT");
            var size = rd.ReadUInt32();
            var sectStart = rd.BaseStream.Position;
            var EnvStore = new Queue<JInstrumentEnvelopev2>();
         
            while ((rd.BaseStream.Position - sectStart) < size) 
                EnvStore.Enqueue(JInstrumentEnvelopev2.CreateFromStream(rd));

            Envelopes = new();

            var total = EnvStore.Count;
            for (int i = 0; i < total; i++) {
                var cEnv = EnvStore.Dequeue();
                Envelopes[cEnv.mBaseAddress - (envTableOffset  + 8)] = cEnv;
            }
        }

        private void loadOscillators(BeBinaryReader rd, int oscTableOffset)
        {
            rd.BaseStream.Position = oscTableOffset;
            if (rd.ReadInt32() != OSCT)
                throw new Exception("Expected OSCT");
            var size = rd.ReadInt32();
            var count = rd.ReadInt32();

            if (count == 0)
                return;

            Oscillators = new();
            for (int i = 0; i < count; i++)
                Oscillators[i] = JInstrumentOscillatorv2.CreateFromStream(rd, this, 0);
        }

        private void loadRandEffs(BeBinaryReader rd, int randTableOffset)
        {
            rd.BaseStream.Position = randTableOffset;

            if (rd.ReadInt32() != RAND)
                throw new Exception("Expected RAND");
            var size = rd.ReadInt32();
            var count = rd.ReadInt32();

            if (count == 0)
                return;

            RandomEffects= new();
            for (int i = 0; i < count; i++)
                RandomEffects[i] = JInstrumentRandEffectv2.CreateFromStream(rd);
        }

        private void loadPmaps(BeBinaryReader rd, int pmapTableOffset)
        {
            rd.BaseStream.Position = pmapTableOffset;
            if (rd.ReadInt32() != PMAP)
                throw new Exception("Expected PMAP");

            var size = rd.ReadInt32();
            var count = rd.ReadInt32();

            if (count == 0)
                return;

            PercussionMaps = new JInstrumentPercussionMapv2[count];
            for (int i = 0; i < count; i++)
                PercussionMaps[i] = JInstrumentPercussionMapv2.CreateFromStream(rd);

        }

        private void loadPercussion(BeBinaryReader rd, int percTableOffset)
        {
            rd.BaseStream.Position = percTableOffset;
            if (rd.ReadInt32() != PERC)
                throw new Exception("Expected PMAP");

            var size = rd.ReadInt32();
            var count = rd.ReadInt32();

            if (count == 0)
                return;

            Percussions = new JPercussionInstrumentv2[count];
            for (int i = 0; i < count; i++)
                Percussions[i] = JPercussionInstrumentv2.CreateFromStream(rd, this);
        }

        private void loadInstruments(BeBinaryReader rd, int instTableOffset)
        {
            rd.BaseStream.Position = instTableOffset;
            if (rd.ReadInt32() != INST)
                throw new Exception("Expected INST");
            var size = rd.ReadInt32();
            var count = rd.ReadInt32();

            if (count == 0)
                return;

            Programs = new JStandardInstrumentv2[count];
            for (int i = 0;i < count;i++)
                Programs[i] = JStandardInstrumentv2.CreateFromStream(rd, this);
        }

        private void loadSensEffs(BeBinaryReader rd, int sensTableOffset)
        {
            rd.BaseStream.Position = sensTableOffset;

            if (rd.ReadInt32() != SENS)
                throw new Exception("Expected SENS");
            var size = rd.ReadInt32();
            var count = rd.ReadInt32();

            if (count == 0)
                return;

            SenseEffects = new();
            for (int i = 0; i < count; i++)
                SenseEffects[i] = JInstrumentSenseEffectv2.CreateFromStream(rd);
        }


        private void loadListMap(BeBinaryReader rd, int listTableOffset)
        {
            rd.BaseStream.Position = listTableOffset;
            if (rd.ReadInt32() != LIST)
                throw new Exception("Expected LIST");
            var size = rd.ReadInt32();
            var count = rd.ReadInt32();

            if (count == 0)
                return;

            Instruments = new JInstrument[count];

            var listPointers = util.readInt32Array(rd, count);

            for (int lp = 0; lp < listPointers.Length; lp++)
            {
                var cont = false;
                var currentListPointer = listPointers[lp];

                if (currentListPointer == 0) // There are empty slots in the instrument list... they're 0x00
                    continue;
               
                for (int i = 0; i < Programs.Length; i++)
                    if (Programs[i].mBaseAddress ==currentListPointer)
                    {
                        Instruments[lp] = Programs[i];
                        cont = true; 
                        break;
                    }

                if (cont) // If we found it in instruments list we don't need to look through the percussion list. 
                    continue; 

                for (int i = 0; i < Percussions.Length; i++)
                    if (Percussions[i].mBaseAddress == currentListPointer)
                    {
                        Instruments[lp] = Percussions[i];
                        break;
                    }
            }
        }


        public static JInstrumentBankv2 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentBankv2();
            b.loadFromStream(reader);
            return b;
        }
    }


    public class JInstrumentEnvelopev2 : JEnvelope
    {
        private void loadFromStream(BeBinaryReader reader)
        {
            var origPos = reader.BaseStream.Position;

            mBaseAddress = (int)origPos;
            int count = 0;
            while (reader.ReadUInt16() < 0xB)
            {
                reader.ReadUInt32();
                count++;
            }
            // We need to account for the last vector in the array, so increment count + 1
            count++;
            reader.BaseStream.Position = origPos;
            points = new JEnvelopeVector[count];
            for (int i = 0; i < count; i++)
                points[i] = new JEnvelopeVector { Mode = reader.ReadUInt16(), Delay = reader.ReadUInt16(), Value = reader.ReadInt16() };
        }
        public static JInstrumentEnvelopev2 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentEnvelopev2();
            b.loadFromStream(reader);
            return b;
        }
    }


    public class JInstrumentRandEffectv2 : JInstrumentRandomEffect
    {
        private const int Rand = 0x52616E64;
        
        private void loadFromStream(BeBinaryReader reader)
        {
            mBaseAddress = (int)reader.BaseStream.Position;
            if (reader.ReadInt32() != Rand)
                throw new Exception("Expected 'Rand'");
            Target = reader.ReadByte();
            reader.ReadBytes(3);
            Floor = reader.ReadSingle();
            Ceiling = reader.ReadSingle();
        }
        public static JInstrumentRandEffectv2 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentRandEffectv2();
            b.loadFromStream(reader);
            return b;
        }
    }




    public class JInstrumentSenseEffectv2 : JInstrumentSenseEffect
    {
        private const int Sens = 0x53656E73;
        
        private void loadFromStream(BeBinaryReader reader)
        {
            mBaseAddress = (int)reader.BaseStream.Position;
            if (reader.ReadInt32() != Sens)
                throw new Exception("Expected 'Sens'");
            Target = reader.ReadByte();
            Register = reader.ReadByte();
            Key = reader.ReadByte();
            reader.ReadBytes(1);
            Floor = reader.ReadSingle();
            Ceiling = reader.ReadSingle();
        }
        public static  JInstrumentSenseEffectv2 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentSenseEffectv2();
            b.loadFromStream(reader);
            return b;
        }
    }

    public class JInstrumentOscillatorv2 : JOscillator
    {
        private const int Osci = 0x4F736369; // Oscillator

        private void loadFromStream(BeBinaryReader reader, InstrumentBank bank, int seekbase)
        {
            mBaseAddress = (int)reader.BaseStream.Position;
            if (reader.ReadInt32() != Osci)
                throw new Exception("Expected Osci");
            Target = reader.ReadByte();
            reader.ReadBytes(3);
            Rate = reader.ReadSingle();

            // Envelopes must be loaded before oscillators, since oscillators reference the envelopes!
            var AttackEnvelopeID = reader.ReadInt32();
            Attack = bank.Envelopes[AttackEnvelopeID];

            var ReleaseEnvelopeID = reader.ReadInt32();
            Release = bank.Envelopes[ReleaseEnvelopeID];

            Width = reader.ReadSingle();
            Vertex = reader.ReadSingle();
        }
        public static JInstrumentOscillatorv2 CreateFromStream(BeBinaryReader reader,InstrumentBank bank, int seekbase)
        {
            var b = new JInstrumentOscillatorv2();
            b.loadFromStream(reader, bank, seekbase);
            return b;
        }
    }

  
    public class JStandardInstrumentv2  : JInstrument
    {
        private const int Inst = 0x496E7374; // Instrument

        private void loadFromStream(BeBinaryReader reader, InstrumentBank ibnk)
        {
            mBaseAddress = (int)reader.BaseStream.Position;
            if (reader.ReadInt32() != Inst)
                throw new Exception("Expected 'Inst'");
            var osciCount = reader.ReadInt32();
            var OscillatorIndices = util.readInt32Array(reader, osciCount);

            for (int i = 0; i < OscillatorIndices.Length; i++)
                Oscillators.Add(ibnk.Oscillators[OscillatorIndices[i]]);
               
       

            var randCount = reader.ReadInt32(); // Uh... evil?
            var EffectIndices = util.readInt32Array(reader, randCount);

            var keyRegCount = reader.ReadInt32();  
            Keys = new JInstrumentKeyRegionv2[keyRegCount];
            for (int i=0; i<keyRegCount; i++) 
                Keys[i] = JInstrumentKeyRegionv2.CreateFromStream(reader);

            Volume = reader.ReadSingle();
            Pitch = reader.ReadSingle();    
        }
        public static JStandardInstrumentv2 CreateFromStream(BeBinaryReader reader, InstrumentBank ibnk)
        {
            var b = new JStandardInstrumentv2();
            b.loadFromStream(reader, ibnk);
            return b;
        }
    }


    public class JInstrumentPercussionMapv2 : JKeyRegion
    {
        private const int Pmap = 0x506D6170;

        private void loadFromStream(BeBinaryReader reader)
        {
       
            mBaseAddress = (int)reader.BaseStream.Position;

            if (reader.ReadInt32() != Pmap)
                throw new Exception("Expected 'Pmap'");
            Volume = reader.ReadSingle();
            Pitch = reader.ReadSingle();
            Unknown = reader.ReadSingle();
            var oscCount = reader.ReadInt32();
            if (oscCount > 0)
                throw new Exception("libjaudio: Unexpected condition Pmap.oscCount > 0");

            var velRegCount = reader.ReadInt32();
            Velocities = new JInstrumentVelocityRegionv2[velRegCount];
            for (int i=0; i < velRegCount; i++)
                Velocities[i] = JInstrumentVelocityRegionv2.CreateFromStream(reader);
        }
        
        public static JInstrumentPercussionMapv2 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentPercussionMapv2();
            b.loadFromStream(reader);
            return b;
        }
    }

    public class JPercussionInstrumentv2 : JInstrument
    {
        private const int Perc = 0x50657263; // Percussion 
 
        private void loadFromStream(BeBinaryReader reader, JInstrumentBankv2 bank)
        {
            mBaseAddress = (int)reader.BaseStream.Position;

            if (reader.ReadInt32() != Perc)
                throw new Exception("Expected 'Perc'");
            var count = reader.ReadInt32();
            var ptrs = util.readInt32Array(reader, count);
            var anchor = reader.BaseStream.Position;

            Keys = new JInstrumentPercussionMapv2[count];
            for (int i=0; i < ptrs.Length; i++)
            {
                if (ptrs[i] <= 0)
                    continue;
                for (int x=0; x < bank.PercussionMaps.Length; x++)
                {
                    var map = bank.PercussionMaps[x];
                    if (map.mBaseAddress == ptrs[i])
                        Keys[i] = map;
                }
                if (Keys[i] == null)
                    throw new Exception("Cannot find map for percussion pointer!");
            }
            reader.BaseStream.Position = anchor;
        }

        public override JKeyRegion getKey(int key)
        {
            if (Keys.Length <= key | Keys[key] == null)
                return null;
            return Keys[key];
        }

        public static JPercussionInstrumentv2 CreateFromStream(BeBinaryReader reader, JInstrumentBankv2 bank)
        {
            var b = new JPercussionInstrumentv2();
            b.loadFromStream(reader, bank);
            return b;
        }
    }

    public class JInstrumentKeyRegionv2 : JKeyRegion
    {
        private void loadFromStream(BeBinaryReader reader)
        {
            BaseKey = reader.ReadByte();
            reader.ReadBytes(3);
            var count = reader.ReadInt32();
            Velocities = new JInstrumentVelocityRegionv2[count];
            for (int i = 0; i < count; i++) 
                Velocities[i] = JInstrumentVelocityRegionv2.CreateFromStream(reader);
        }
        public static JInstrumentKeyRegionv2 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentKeyRegionv2();
            b.loadFromStream(reader);
            return b;
        }
    }

    public class JInstrumentVelocityRegionv2 : JVelocityRegion
    {
        private void loadFromStream(BeBinaryReader reader)
        {
            Velocity = reader.ReadByte();
            reader.ReadBytes(3);
            WSYSID = reader.ReadUInt16();
            WaveID = reader.ReadUInt16();
            Volume = reader.ReadSingle();
            Pitch = reader.ReadSingle();
        }
        public static JInstrumentVelocityRegionv2 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentVelocityRegionv2();
            b.loadFromStream(reader);
            return b;
        }
    }
}
