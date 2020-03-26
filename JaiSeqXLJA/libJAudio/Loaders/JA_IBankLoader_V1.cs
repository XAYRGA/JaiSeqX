//#define OSCILLATOR_DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libJAudio;
using System.IO;
using Be.IO;

namespace libJAudio.Loaders
{
    public class JA_IBankLoader_V1
    {
        private const int IBNK = 0x49424e4b;
        private const int BANK = 0x42414E4B;
        private const int INST = 0x494E5354;
        private const int PERC = 0x50455243;
        private const int PER2 = 0x50455232;
        /*
            JAIV1 IBNK structure
            0x00 int32 0x49424e4b 'IBNK'
            0x04 int32 Section Size
            0x08 int32 Global Bank ID
            0x0C int32 IBankFlags 
            0x10 byte[0x14] padding; 
            0x24 TYPE BANK
        */
        private short currentInstID = 0; // runtime runtime runtime 
        private short currentBankID = 0; // also runtime -- just for tracking
        public JIBank loadIBNK(BeBinaryReader binStream,int Base)
        {
            var RetIBNK = new JIBank();
            binStream.BaseStream.Position = Base;
            long anchor = 0; // Return / Seekback anchor
            //binStream.BaseStream.Seek(-4, SeekOrigin.Current);
            if (binStream.ReadInt32() != IBNK) // Check if first 4 bytes are IBNK
                throw new InvalidDataException("Data is not an IBANK");
            var SectionSize = binStream.ReadUInt32(); // Read IBNK Size
            var IBankID = binStream.ReadInt32(); // Read the global IBankID
            currentBankID =(short)IBankID;
            var IBankFlags = binStream.ReadUInt32(); // Flags?
            binStream.BaseStream.Seek(0x10, SeekOrigin.Current); // Skip Padding
            anchor = binStream.BaseStream.Position;
            var Instruments = loadBank(binStream, Base); // Load the instruments

            RetIBNK.id = IBankID; // Store bankID
            RetIBNK.Instruments = Instruments; // Store instruments

            return RetIBNK;
        }

        /* 
            JAIV1 BANK structure 
            0x00 int32 0x42414E4B 'BANK';
            0x04 int32[0xF0] InstrumentPointers
            ---- NOTE: If the instrument pointer is 0, then the instrument for that index is NULL!
        */
        public JInstrument[] loadBank(BeBinaryReader binStream, int Base)
        {
            if (binStream.ReadUInt32() != BANK) // Check if first 4 bytes are BANK
                throw new InvalidDataException("Data is not a BANK");
            var InstrumentPoiners = new int[0xF0]; // Table of pointers for the instruments;
            var Instruments = new JInstrument[0xF0];
            InstrumentPoiners = Helpers.readInt32Array(binStream, 0xF0); //  Read instrument pointers.
            for (int i=0; i < 0xF0; i++)
            {
                binStream.BaseStream.Position = InstrumentPoiners[i] + Base; // Seek to pointer position
                var type = binStream.ReadInt32(); // Read type
                binStream.BaseStream.Seek(-4, SeekOrigin.Current); // Seek back 4 bytes to undo header read. 
                currentInstID = (short)i;
                switch (type)
                {
                    case INST:
                        Instruments[i] = loadInstrument(binStream, Base); // Load instrument
                        break;
                    case PER2:
                        Instruments[i] = loadPercussionInstrument(binStream, Base); // Load percussion
                        break;
                    default:
                        // no action, we don't know what it is and it won't misalign.
                        break;
                }
            }
            return Instruments;
        }
        /* 
            JAIV1 INST Structure 
            0x00 int32 0x494E5354 'INST'
            0x04 int32 0 - unused?
            0x08 float frequencyMultiplier
            0x0C float gainMultiplier
            0x10 int32 oscillator table offset
            0x14 int32 oscillator table count
            0x18 int32 effect table offset 
            0x1C int32 effect table size 
            0x20 int32 sensor object table
            0x24 int32 sensor object table count
            0x28 int32 key_region_count
            0x2C *KeyRegion[key_region_count]

        */

        public JInstrument loadInstrument(BeBinaryReader binStream, int Base)
        {
            var Inst = new JInstrument();
            if (binStream.ReadUInt32() != INST) // Check if first 4 bytes are INST
                throw new InvalidDataException("Data is not an INST");
            binStream.ReadUInt32(); // oh god oh god oh god its null
            Inst.Pitch = binStream.ReadSingle();
            Inst.Volume = binStream.ReadSingle();
            var osc1Offset = binStream.ReadUInt32(); 
            var osc2Offset = binStream.ReadUInt32(); 
            binStream.ReadUInt32(); // *NOT IMPLEMENTED* //
            binStream.ReadUInt32(); // *NOT IMPLEMENTED* //
            binStream.ReadUInt32(); // *NOT IMPLEMENTED* //
            binStream.ReadUInt32(); // *NOT IMPLEMENTED* //
            // * trashy furry screaming * //
            int keyregCount = binStream.ReadInt32(); // Read number of key regions
            JInstrumentKey[] keys = new JInstrumentKey[0x81]; // Always go for one more.
            int[] keyRegionPointers = new int[keyregCount];
            keyRegionPointers = Helpers.readInt32Array(binStream, keyregCount);
            var keyLow = 0; // For region spanning. 
            for (int i=0; i < keyregCount; i++) // Loop through all pointers.
            {
                binStream.BaseStream.Position = keyRegionPointers[i] + Base; // Set position to key pointer pos (relative to base)
                var bkey = readKeyRegion(binStream, Base); // Read the key region
                for (int b=0; b < bkey.baseKey - keyLow; b++) 
                {
                    //  They're key regions, so we're going to have a toothy / gappy piano. So we need to span the missing gaps with the previous region config.
                    // This means that if the last key was 4, and the next key was 8 -- whatever parameters 4 had will span keys 4 5 6 and 7. 
                    keys[b + keyLow] = bkey; // span the keys
                    keys[127] = bkey;
                }
                keyLow = bkey.baseKey; // Store our last key 
            }
            Inst.Keys = keys;
            byte oscCount = 0;
            if (osc1Offset > 0)
                oscCount++;
            if (osc2Offset > 0)
                oscCount++;
            Inst.oscillatorCount = oscCount; // Redundant?
            Inst.oscillators = new JOscillator[oscCount]; // new oscillator array
            if (osc1Offset!=0) // if the oscillator isnt null
            {
                binStream.BaseStream.Position = osc1Offset + Base; // seek to it's position
                Inst.oscillators[0] = loadOscillator(binStream, Base); // then load it.
            }
            if (osc2Offset != 0) // if the second oscillator isn't null
            {
                binStream.BaseStream.Position = osc2Offset + Base; // seek to its position
                Inst.oscillators[1] = loadOscillator(binStream, Base); // and load it.
            }
            return Inst;
        }


        /*
        JAIV1 Oscillator Vector Structure 
            These are weird. so i'll just do it like this
            short mode
            short time
            short value

            when you read anything over 8 for the mode, read the last two shorts then stop reading -- so the last value in the array would be

            0x000F, 0x0000, 0x0000
        */
        public JEnvelope readEnvelope(BeBinaryReader binStream, int Base)
        {
            var len = 0;
            // This function cheats a little bit :) 
            // We dive in not knowing the length of the table -- the table is stopped whenever one of the MODE bytes is more than 0xB. 
            var seekBase = binStream.BaseStream.Position;
            for (int i=0; i < 10; i++)
            {
                var mode = binStream.ReadInt16(); // reads the first 2 bytes of the table
                len++; // The reason we do this, is because we still need to read the loop flag, or stop flag.
                if (mode < 0xB) // This determines the mode, the mode will always be less than 0xB -- unless its telling the table to end. 
                {
                    // If it is, then we definitely want to read this entry, so we increment the counter
                    binStream.ReadInt32(); // Then skip the actual entry data
                }
                else // The value was over 10 
                {           
                    break;  // So we need to stop the loop
                }
            }
            binStream.BaseStream.Position = seekBase;  // After we have an idea how big the table is -- we want to seek back to the beginning of it.
            JEnvelopeVector[] OscVecs = new JEnvelopeVector[len]; // And create an array the size of our length.

            for (int i=0; i < len - 1;i++) // we read - 1 because we don't want to read the end value yet
            {              
                var vector = new JEnvelopeVector
                {
                    mode = (JEnvelopeVectorMode)binStream.ReadInt16(), // Read the values of each into their places
                    time = binStream.ReadInt16(), // read time 
                    value = binStream.ReadInt16() // read value
                };
                OscVecs[i] = vector;
            } // Go down below for the last vector, after sorting

            // todo: Figure out why this doesn't sort right? 
             
            // -1 is so we don't sort the last object in the array, because its null. We're sorting from the bottom up.
            for (int i=0; i < len - 1; i++) // a third __fucking iteration__ on these stupid vectors.
            {
                for (int j = 0; j < len -1 ; j++)
                {
                    var current = OscVecs[i]; // Grab current oscillator vector, notice the for loop starts at 1
                    var cmp = OscVecs[j]; // Grab the previous object
                    if (cmp.time > current.time) // if its time is greater than ours
                    {
                        OscVecs[j] = current; // shift us down
                        OscVecs[i] = cmp; // shift it up
                    }
                }
            } //*/
            // Now that we've sorted the vectors because nintendo packs them out of fucking order.
            // We can add the hold / stop vector :D
            // We havent advanced any more bytes by the way, so we're still at the end of that vector array from before.

            var lastVector = OscVecs[OscVecs.Length - 2]; // -2 gets the last indexed object.
            // This is disgusting, i know.
            OscVecs[OscVecs.Length - 1] = new JEnvelopeVector
            {
                mode = (JEnvelopeVectorMode)binStream.ReadInt16(), // Read the values of each into their places
                time = (short)(lastVector.time), // read time 
                value = lastVector.value // read value
            };
            // Setting up references. 
            // can only be done after sorting :v...
            for (int idx = 0; idx < OscVecs.Length -1 ; idx++)
            {
                    OscVecs[idx].next = OscVecs[idx + 1]; // current vector objects next is the one after it.
            }
            var ret = new JEnvelope();
            ret.vectorList = OscVecs;
            return ret; // finally, return. 
        }


        /*
         JAIV1 Oscillator Format 
         0x00 - byte mode 
         0x01 - byte[3] unknown
         0x04 - float rate
         0x08 - int32 attackVectorOffset
         0x0C - int32 releaseVectorOffset
         0x10 - float width
         0x14 - float vertex
        */    
        public JOscillator loadOscillator(BeBinaryReader binStream, int Base)
        {
            var Osc = new JOscillator(); // Create new oscillator
            var target = binStream.ReadByte(); // load target -- what is it affecting?
            binStream.BaseStream.Seek(3, SeekOrigin.Current); // read 3 bytes?
            Osc.rate = binStream.ReadSingle(); // Read the rate at which the oscillator progresses -- this will be relative to the number of ticks per beat.
            var attackSustainTableOffset = binStream.ReadInt32(); // Offset of AD table
            var releaseDecayTableOffset = binStream.ReadInt32(); // Offset of SR table
            Osc.Width = binStream.ReadSingle(); // We should load these next, this is the width, ergo the value of the oscillator at 32768. 
            Osc.Vertex = binStream.ReadSingle();  // This is the vertex, the oscillator will always cross this point. 
            // To determine the value of an oscillator, it's Vertex + Width*(value/32768) -- each vector should progress the value, depending on the mode. 
            if (attackSustainTableOffset > 0) // first is AS table
            {
                binStream.BaseStream.Position = attackSustainTableOffset + Base; // Seek to the vector table
                Osc.envelopes[0] = readEnvelope(binStream, Base); // Load the table
            }
            if (releaseDecayTableOffset > 0) // Next is RD table
            {
                binStream.BaseStream.Position = releaseDecayTableOffset + Base; // Seek to the vector and load it
                Osc.envelopes[1] = readEnvelope(binStream, Base); // loadddd
            }
            Osc.target = (JOscillatorTarget)target;
            return Osc;
        }


        /* 
            JAIV1 KeyRegion Structure
            0x00 byte baseKey 
            0x01 byte[0x3] unused;
            0x04 int32 velocityRegionCount
            *VelocityRegion[velocityRegionCount] velocities;
        */
        public JInstrumentKey readKeyRegion(BeBinaryReader binStream, int Base)
        {
            JInstrumentKey newKey = new JInstrumentKey();
            newKey.Velocities = new JInstrumentKeyVelocity[0x81]; // Create region array
            //-------
            newKey.baseKey = binStream.ReadByte(); // Store base key
            binStream.BaseStream.Seek(3, SeekOrigin.Current); ; // Skip 3 bytes
            var velRegCount = binStream.ReadInt32(); // Grab vel region count
            int[] velRegPointers = new int[velRegCount]; // Create Pointer array
            velRegPointers = Helpers.readInt32Array(binStream, velRegCount);
            var velLow = 0;  // Again, these are regions -- see LoadInstrument for this exact code ( a few lines above ) 
            for (int i=0; i < velRegCount; i++)
            {
                binStream.BaseStream.Position = velRegPointers[i] + Base;
                var breg = readKeyVelRegion(binStream, Base);  // Read the vel region.
                for (int b = 0; b <  breg.baseVel - velLow; b ++)
                {
                    //  They're velocity regions, so we're going to have a toothy / gappy piano. So we need to span the missing gaps with the previous region config.
                    // This means that if the last key was 4, and the next key was 8 -- whatever parameters 4 had will span keys 4 5 6 and 7. 
                    newKey.Velocities[b] = breg;
                    newKey.Velocities[127] = breg;
                }
                velLow = breg.baseVel;
            }
            return newKey;
        }

        /* 
            JAIV1 Velocity Region Structure
           0x00 byte baseVelocity;
           0x04 byte[0x03] unused;
           0x07 short wsysID;
           0x09 short waveID;
           0x0D float Volume; 
           0x11 float Pitch;
        */

        public JInstrumentKeyVelocity readKeyVelRegion(BeBinaryReader binStream, int Base)
        {
            JInstrumentKeyVelocity newReg = new JInstrumentKeyVelocity();
            newReg.baseVel = binStream.ReadByte();
            binStream.BaseStream.Seek(3, SeekOrigin.Current); ; // Skip 3 bytes.
            newReg.velocity = newReg.baseVel;
            newReg.wsysid = binStream.ReadInt16();
            newReg.wave = binStream.ReadInt16();
            newReg.Volume = binStream.ReadSingle();
            newReg.Pitch = binStream.ReadSingle();
            return newReg;
        }

        /* 
         JAIV1 PER2 Structure 
         0x00 int32 0x50455232 'PER2'
         0x04 byte[0x84] unused; // the actual fuck? Waste of 0x84 perfectly delicious bytes.
         0x8C *PercussionKey[100]      

         PER2 PercussionKey Structure
             float pitch;
             float volume;
             byte[0x8] unused?
             int32 velocityRegionCount
             *VelocityRegion[velocityRegionCount]  velocities

        */

        public JInstrument loadPercussionInstrument(BeBinaryReader binStream, int Base)
        {
            var Inst = new JInstrument();
            if (binStream.ReadUInt32() != PER2) // Check if first 4 bytes are PER2
                throw new InvalidDataException("Data is not an PER2");
            Inst.Pitch = 1.0f;
            Inst.Volume = 1.0f;
            Inst.IsPercussion = true;
            binStream.BaseStream.Seek(0x84, SeekOrigin.Current);
            JInstrumentKey[] keys = new JInstrumentKey[100];
            int[] keyPointers = new int[100];
            keyPointers = Helpers.readInt32Array(binStream, 100); // read the pointers.
          
            for (int i = 0; i < 100; i++) // Loop through all pointers.
            {
                if (keyPointers[i] == 0 )
                {
                    continue;
                }
                binStream.BaseStream.Position = keyPointers[i] + Base; // Set position to key pointer pos (relative to base)     
                var newKey = new JInstrumentKey();
                newKey.Pitch = binStream.ReadSingle(); // read the pitch
                newKey.Volume = binStream.ReadSingle(); // read the volume
                binStream.BaseStream.Seek(8, SeekOrigin.Current); // runtime values, skip
                var velRegCount = binStream.ReadInt32(); // read count of regions we have
                newKey.Velocities = new JInstrumentKeyVelocity[0xff]; // 0xFF just in case.
                int[] velRegPointers = Helpers.readInt32Array(binStream,velRegCount);
             
                var velLow = 0;  // Again, these are regions -- see LoadInstrument for this exact code ( a few lines above ) 
                for (int b = 0; b < velRegCount; b++)
                {
                    binStream.BaseStream.Position = velRegPointers[b] + Base;
                    var breg = readKeyVelRegion(binStream, Base);  // Read the vel region.
                    for (int c = 0; c < breg.baseVel - velLow; c++)
                    {
                        //  They're velocity regions, so we're going to have a toothy / gappy piano. So we need to span the missing gaps with the previous region config.
                        // This means that if the last key was 4, and the next key was 8 -- whatever parameters 4 had will span keys 4 5 6 and 7. 
                        newKey.Velocities[c] = breg; // store the  region
                        newKey.Velocities[127] = breg;
                    }
                    velLow = breg.baseVel; // store the velocity for spanning
                }
                keys[i] = newKey;
            }
            Inst.Keys = keys;
            return Inst;
        }

    }
}
