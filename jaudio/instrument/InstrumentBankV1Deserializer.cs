using jaudio.instrument;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xayrga.byteglider;

namespace jaudio.instrument
{
    internal class InstrumentBankV1Deserializer 
    {
        private const int IBNK = 0x49424e4b;
        private const int BANK = 0x42414E4B;

        private const int INST = 0x494E5354;
        private const int PER2 = 0x50455232;

        private const int BANK_OFFSET = 0x20;
        private const int BANK_INST_COUNT = 0xF0;
        private const int PER2_INST_COUNT = 0x80;

        private bgReader reader;
        private JInstrumentBank bank;


        public InstrumentBankV1Deserializer(bgReader stream) 
        { 
            reader = stream;
            bank = new JInstrumentBank();
        }

        public JInstrumentBank readBank()
        {
            reader.SetBaseCurrentPos();

            if (reader.ReadInt32BE() != IBNK)
                throw new Exception("Not an IBNK");
            
            bank.Size = reader.ReadInt32BE();
            bank.ID = reader.ReadInt32BE();

            reader.Seek(BANK_OFFSET);

            if (reader.ReadInt32BE() != BANK)
                throw new Exception("Invalid V1 IBNK");

            var instrumentOffsets = reader.ReadInt32ArrayBE(BANK_INST_COUNT);
            for (int i = 0; i < BANK_INST_COUNT; i++)
                if (instrumentOffsets[i] != 0) {
                    reader.Seek(instrumentOffsets[i]);
                    bank.Instruments[i] = readInstrument();
                }

            return bank;
        }


        public JInstrument readInstrument()
        {
            JInstrument inst;
            var type = reader.ReadUInt32BE();
            switch (type)
            {
                case INST:
                    inst = loadINST();
                    break;
                case PER2:
                    inst = loadPER2();
                    break;
                default:
                    throw new Exception($"Unrecognized instrument type {type:X}");
            }

            return inst;
        }

        private JInstrument loadPER2()
        {
            JPercussionInstrument inst = new JPercussionInstrument();
            inst.Percussion = true;
            reader.Skip(4); // Empty?
            byte[] insFlg1 = reader.ReadBytes(PER2_INST_COUNT);
            int[] keyOffsets = reader.ReadInt32ArrayBE(PER2_INST_COUNT);
            byte[] instPans = reader.ReadBytes(PER2_INST_COUNT);
            byte[] attackRelease = reader.ReadBytes(PER2_INST_COUNT * 2);

            for (byte i = 0; i < PER2_INST_COUNT; i++)
            {
                var offset = keyOffsets[i];

                if (offset == 0)
                    continue;

                var flg1 = insFlg1[i];  // I don't know what this is used for.           
                var pan = instPans[i];
                var attack = attackRelease[i * 2];
                var release = attackRelease[i * 2 + 1];


                reader.Seek(offset);

                var keyReg = loadPercussionEntry();
                keyReg.Percussion = true;
                keyReg.Pan = pan; 
                keyReg.Attack = attack;
                keyReg.Release = release;
                keyReg.BaseKey = i;        
                inst.Keys.Add(keyReg);
            }
            return inst;
        }
        private JInstrument loadINST()
        {
            JInstrument inst = new JInstrument();
            reader.Skip(4); // Empty?
            inst.Pitch = reader.ReadSingleBE();
            inst.Volume = reader.ReadSingleBE();

            // loading oscillators
            var effOffset = 0;
            for (int i = 0; i < 2; i++)
                if ((effOffset = reader.ReadInt32BE()) != 0)
                    inst.Oscillators.Add(effOffset);

            // loading rands
            for (int i = 0; i < 2; i++)
                if ((effOffset = reader.ReadInt32BE()) != 0)
                    inst.RandEffects.Add(effOffset);

            // loading effects
            for (int i = 0; i < 2; i++)
                if ((effOffset = reader.ReadInt32BE()) != 0)
                    inst.SenseEffects.Add(effOffset);

            var keyRegCount = reader.ReadInt32BE();
            var keyRegPointers = reader.ReadInt32ArrayBE(keyRegCount);

            for (int i = 0; i < keyRegCount; i++)
            {
                reader.Seek(keyRegPointers[i]);
                inst.Keys.Add(loadKeyRegion());
            }

            // Now we should insert the oscillators into the BANK

            for (int i = 0; i < inst.Oscillators.Count; i++)
            {
                var offset = inst.Oscillators[i]; // We're using the offset as the key in this case, most effective way to do this.
                if (bank.Oscillators.ContainsKey(offset))
                    continue; // We can just skip it, we've already referenced this oscillator.
                reader.Seek(offset);
                bank.Oscillators[offset] = loadOscillator();
            }

            for (int i = 0; i < inst.SenseEffects.Count; i++)
            {
                var offset = inst.SenseEffects[i]; // We're using the offset as the key in this case, most effective way to do this.
                if (bank.SenseEffects.ContainsKey(offset))
                    continue; // We can just skip it, we've already referenced this oscillator.
                reader.Seek(offset);
                bank.SenseEffects[offset] = loadSenseEffect();
            }

            for (int i = 0; i < inst.RandEffects.Count; i++)
            {
                var offset = inst.RandEffects[i]; // We're using the offset as the key in this case, most effective way to do this.
                if (bank.RandEffects.ContainsKey(offset))
                    continue; // We can just skip it, we've already referenced this oscillator.
                reader.Seek(offset);
                bank.RandEffects[offset] = loadRandEffect();
            }

            return inst;
        }

        public JInstrumentSenseEffect loadSenseEffect()
        {
            var addr = (int)reader.GetPosition();
            var sens = new JInstrumentSenseEffect();
            sens.Target = (EJInstrumentEffectTarget)reader.ReadByte();

            var trig = reader.ReadByte();
            Console.WriteLine($"Trigger = {trig}");
            sens.Trigger = (JInstrumentSenseEffect.ESenseEffectTrigger)trig;
            sens.Key = reader.ReadByte();
            reader.Skip(1);
            sens.Floor = reader.ReadSingleBE();
            sens.Ceiling = reader.ReadSingleBE();
            return sens;
        }


        public JInstrumentRandEffect loadRandEffect()
        {
            var addr = (int)reader.GetPosition();
            var sens = new JInstrumentRandEffect();
            sens.Target = (EJInstrumentEffectTarget)reader.ReadByte();
            reader.Skip(3);
            sens.Floor = reader.ReadSingleBE();
            sens.Ceiling = reader.ReadSingleBE();
            return sens;
        }

        public JInstrumentOscillator loadOscillator()
        {
            var osci = new JInstrumentOscillator();
            osci.Target = (EJInstrumentEffectTarget)reader.ReadByte();
            reader.Skip(3);
            osci.Rate = reader.ReadSingleBE();
            osci.AttackEnvelope = reader.ReadInt32BE();
            osci.ReleaseEnvelope = reader.ReadInt32BE();
            osci.Width = reader.ReadSingleBE();
            osci.Base = reader.ReadSingleBE();

            reader.Seek(osci.AttackEnvelope);
            if (osci.AttackEnvelope > 0)
                loadEnvelope();

            reader.Seek(osci.ReleaseEnvelope);
            if (osci.ReleaseEnvelope > 0)
                loadEnvelope();

            return osci;
        }

        public JEnvelopeVector[] loadEnvelope()
        {
            var offset = reader.GetPosition();
            var envelopeData = new List<JEnvelopeVector>();
            short mode = 0;
            while ((mode = reader.ReadInt16BE()) < 0xB) // JEnvelopeVector.JEnvelopeVectorMode.Loop == 0xB, loop and everything above "stops" an envelope.
                envelopeData.Add( new JEnvelopeVector()
                {
                    Mode = (JEnvelopeVector.JEnvelopeVectorMode)mode,
                    Duration = reader.ReadInt16BE(),
                    Value = reader.ReadInt16BE()

                });

            // we stopped before the last vector. 
            envelopeData.Add(new JEnvelopeVector()
            {
                Mode = (JEnvelopeVector.JEnvelopeVectorMode)mode,
                Duration = reader.ReadInt16BE(),
                Value = reader.ReadInt16BE()

            });

            var data = envelopeData.ToArray();
            bank.Envelopes[(int)offset] = data;
            return data;
        }

        public JInstrument.JKeyRegion loadKeyRegion()
        {
            var keyReg = new JInstrument.JKeyRegion();
            keyReg.BaseKey = reader.ReadByte();
            reader.Skip(3); // align 4, 
            var velRegCount = reader.ReadInt32BE();
            var velRegOffsets = reader.ReadInt32ArrayBE(velRegCount);
            for (int i=0; i < velRegCount; i++)
            {
                reader.Seek(velRegOffsets[i]);
                keyReg.Velocities.Add(loadVelocityRegion());
            }  
            return keyReg;
        }

        public JInstrument.JVelocityRegion loadVelocityRegion()
        {
            var velocityRegion = new JInstrument.JVelocityRegion();
            velocityRegion.Velocity = reader.ReadByte();
            reader.Skip(3); // align 4
            velocityRegion.WSYSID = reader.ReadUInt16BE();
            velocityRegion.WaveID = reader.ReadUInt16BE();
            velocityRegion.Pitch = reader.ReadSingleBE();
            velocityRegion.Volume = reader.ReadSingleBE();
            // NOTE, Perucssion, attack, release, and pan fields are assigned only in the percussion instrument, sorry.
            return velocityRegion;
        }

        public JInstrument.JKeyRegion loadPercussionEntry()
        {
            var keyRegion = new JInstrument.JKeyRegion();
            keyRegion.Pitch = reader.ReadSingleBE();
            keyRegion.Volume = reader.ReadSingleBE();
            reader.Skip(8); // I forget if this contains anything useful, probably not.
            var velRegCount = reader.ReadInt32BE();
            var velRegPtrs = reader.ReadInt32ArrayBE(velRegCount);
            for (int i=0; i < velRegCount; i++)
            {
                reader.Seek(velRegPtrs[i]);
                keyRegion.Velocities.Add(loadVelocityRegion());
            }
            return keyRegion;
        }
    }
}
