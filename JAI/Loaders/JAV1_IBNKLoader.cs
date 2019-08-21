using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JaiSeqX.JAI.Types;
using System.IO;
using Be.IO;

namespace JaiSeqX.JAI.Loaders
{
    public static class JAV1_IBankLoader
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
        public static JIBank loadIBNK(BeBinaryReader binStream,int Base)
        {
            var RetIBNK = new JIBank();
            binStream.BaseStream.Position = Base;
            long anchor = 0; // Return / Seekback anchor
            var HeaderData = 0; // Temporary storage for each sanity check.
            Console.WriteLine("0x{0:X}", binStream.ReadInt32());
            binStream.BaseStream.Seek(-4, SeekOrigin.Current);
            if (binStream.ReadInt32() != IBNK) // Check if first 4 bytes are IBNK
                throw new InvalidDataException("Data is not an IBANK");
            var SectionSize = binStream.ReadUInt32(); // Read IBNK Size
            var IBankID = binStream.ReadInt32(); // Read the global IBankID
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
        public static JInstrument[] loadBank(BeBinaryReader binStream, int Base)
        {
            if (binStream.ReadUInt32() != BANK) // Check if first 4 bytes are BANK
                throw new InvalidDataException("Data is not a BANK");
            var InstrumentPoiners = new int[0xF0]; // Table of pointers for the instruments;
            var Instruments = new JInstrument[0xF0];
            for (int i=0; i < 0xF0; i++)
            {
                InstrumentPoiners[i] = binStream.ReadInt32(); // Load the pointers first.
            }
            for (int i=0; i < 0xF0; i++)
            {
                binStream.BaseStream.Position = InstrumentPoiners[i] + Base; // Seek to pointer position
                var type = binStream.ReadInt32(); // Read type
                binStream.BaseStream.Seek(-4, SeekOrigin.Current); // Seek back 4 bytes to undo header read. 
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

        public static JInstrument loadInstrument(BeBinaryReader binStream, int Base)
        {
            var Inst = new JInstrument();
            if (binStream.ReadUInt32() != INST) // Check if first 4 bytes are INST
                throw new InvalidDataException("Data is not an INST");
            binStream.ReadUInt32(); // oh god oh god oh god its null
            Inst.Pitch = binStream.ReadSingle();
            Inst.Volume = binStream.ReadSingle();

            binStream.ReadUInt32(); // *NOT IMPLEMENTED* //
            binStream.ReadUInt32(); // *NOT IMPLEMENTED* //
            binStream.ReadUInt32(); // *NOT IMPLEMENTED* //
            binStream.ReadUInt32(); // *NOT IMPLEMENTED* //
            binStream.ReadUInt32(); // *NOT IMPLEMENTED* //
            binStream.ReadUInt32(); // *NOT IMPLEMENTED* //
            // * trashy furry screaming * //
            int keyregCount = binStream.ReadInt32(); // Read number of key regions
            JInstrumentKey[] keys = new JInstrumentKey[0x81]; // Always go for one more.
            int[] keyRegionPointers = new int[keyregCount];
            for (int i=0; i < keyregCount; i++)
            {
                keyRegionPointers[i] = binStream.ReadInt32(); // Store the pointers for each key region
            }
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
            return Inst;
        }

        /* 
            JAIV1 KeyRegion Structure
            0x00 byte baseKey 
            0x01 byte[0x3] unused;
            0x04 int32 velocityRegionCount
            *VelocityRegion[velocityRegionCount] velocities;
        */
        public static JInstrumentKey readKeyRegion(BeBinaryReader binStream, int Base)
        {
            JInstrumentKey newKey = new JInstrumentKey();
            newKey.Velocities = new JInstrumentKeyVelocity[0x81]; // Create region array
            //-------
            newKey.baseKey = binStream.ReadByte(); // Store base key
            binStream.BaseStream.Seek(3, SeekOrigin.Current); ; // Skip 3 bytes
            var velRegCount = binStream.ReadInt32(); // Grab vel region count
            int[] velRegPointers = new int[velRegCount]; // Create Pointer array
            for (int i=0; i < velRegCount; i++)
            {
                velRegPointers[i] = binStream.ReadInt32(); // Read all pointers
            }
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

        public static JInstrumentKeyVelocity readKeyVelRegion(BeBinaryReader binStream, int Base)
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

        public static JInstrument loadPercussionInstrument(BeBinaryReader binStream, int Base)
        {
            var Inst = new JInstrument();
            if (binStream.ReadUInt32() != PER2) // Check if first 4 bytes are PER2
                throw new InvalidDataException("Data is not an PER2");
            Inst.Pitch = 1.0f;
            Inst.Volume = 1.0f;
            JInstrumentKey[] keys = new JInstrumentKey[100];
            int[] keyPointers = new int[100];
            for (int i = 0; i < 100; i++)
            {
                keyPointers[i] = binStream.ReadInt32(); // Store the pointers for each key region
            }

          
            for (int i = 0; i < 100; i++) // Loop through all pointers.
            {
                if (keyPointers[i] == 0 )
                {
                    continue;
                }
                binStream.BaseStream.Position = keyPointers[i] + Base; // Set position to key pointer pos (relative to base)     
                var newKey = new JInstrumentKey();
                newKey.Pitch = binStream.ReadSingle();
                newKey.Volume = binStream.ReadSingle();
                binStream.BaseStream.Seek(8, SeekOrigin.Current);
                var velRegCount = binStream.ReadInt32();
                int[] velRegPointers = new int[velRegCount]; // Create Pointer array
                for (int b = 0; b < velRegCount; b++)
                {
                    velRegPointers[i] = binStream.ReadInt32(); // Read all pointers
                }
                var velLow = 0;  // Again, these are regions -- see LoadInstrument for this exact code ( a few lines above ) 
                for (int b = 0; b < velRegCount; b++)
                {
                    binStream.BaseStream.Position = velRegPointers[i] + Base;
                    var breg = readKeyVelRegion(binStream, Base);  // Read the vel region.
                    for (int c = 0; b < breg.baseVel - velLow; c++)
                    {
                        //  They're velocity regions, so we're going to have a toothy / gappy piano. So we need to span the missing gaps with the previous region config.
                        // This means that if the last key was 4, and the next key was 8 -- whatever parameters 4 had will span keys 4 5 6 and 7. 
                        newKey.Velocities[b] = breg;
                        newKey.Velocities[127] = breg;
                    }
                    velLow = breg.baseVel;
                }
            }
            Inst.Keys = keys;


            return Inst;
        }

    }
}
