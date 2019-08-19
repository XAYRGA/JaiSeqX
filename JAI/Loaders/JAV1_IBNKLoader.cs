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
            if (binStream.ReadUInt32() == IBNK) // Check if first 4 bytes are IBNK
                throw new InvalidDataException("Data is not an IBANK");
            var SectionSize = binStream.ReadUInt32(); // Read IBNK Size
            var IBankID = binStream.ReadInt32(); // Read the global IBankID
            var IBankFlags = binStream.ReadUInt32(); // Flags?
            binStream.BaseStream.Seek(0x10, SeekOrigin.Current); // Skip Padding
            anchor = binStream.BaseStream.Position;
            var Instruments = loadBank(binStream, Base); // Load the instruments

            RetIBNK.id = IBankID;
            RetIBNK.Instruments = Instruments;

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
            if (binStream.ReadUInt32() == BANK) // Check if first 4 bytes are BANK
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
            if (binStream.ReadUInt32() == INST) // Check if first 4 bytes are INST
                throw new InvalidDataException("Data is not an INST");
            Inst.Pitch = binStream.ReadSingle();
            Inst.Volume = binStream.ReadSingle();

            binStream.ReadUInt32(); // *NOT IMPLEMENTED* //
            binStream.ReadUInt32(); // *NOT IMPLEMENTED* //
            binStream.ReadUInt32(); // *NOT IMPLEMENTED* //
            binStream.ReadUInt32(); // *NOT IMPLEMENTED* //
            binStream.ReadUInt32(); // *NOT IMPLEMENTED* //
            binStream.ReadUInt32(); // *NOT IMPLEMENTED* //
            // * trashy furry screaming * //




            return Inst;
        }


        public static JInstrument loadPercussionInstrument(BeBinaryReader binStream, int Base)
        {
            var Inst = new JInstrument();

            return Inst;
        }

    }
}
