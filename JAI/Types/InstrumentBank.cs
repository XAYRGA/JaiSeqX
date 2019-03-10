using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;
using System.IO;

namespace JaiSeqX.JAI.Types
{
    public class InstrumentBank
    {
        public int id;

        public Instrument[] Instruments;

        private const uint INST = 0x494E5354;
        private const uint PERC = 0x50455243;
        private const uint PER2 = 0x50455232;

        public void LoadInstrumentBank(BeBinaryReader instReader, JAIVersion version)
        {

            Instruments = new Instrument[0xF0]; // for some reason, they will only ever have 0xF0 instruments in them
          
            if (version==JAIVersion.ONE || version==JAIVersion.TWO)
            {
                loadIBNKJaiV1(instReader); // Both of these use standard ibnk structure. 
            }
      
        }


        private void loadIBNKJaiV1(BeBinaryReader instReader)
        {
            long anchor = 0;
            var BaseAddress = instReader.BaseStream.Position;
            var current_header = 0u;
            current_header = instReader.ReadUInt32(); // read the first 4 byteas
            if (current_header != 0x49424e4b) // Check to see if it equals IBNK
            {
                throw new InvalidDataException("Scanned header is not an IBNK.");
            }
            var size = instReader.ReadUInt32();
            id = instReader.ReadInt32(); // Global virtual ID
            instReader.BaseStream.Seek(0x14, SeekOrigin.Current); // 0x14 bytes always blank 
            current_header = instReader.ReadUInt32(); // We should be reading "BANK"
            for (int inst_id = 0; inst_id < 0xF0; inst_id++)
            {
                var inst_offset = instReader.ReadInt32(); // Read the relative pointer to the instrument
                anchor = instReader.BaseStream.Position; // store the position to jump back into.
                if (inst_offset > 0) // If we have a 0 offset, then the instrument is unassigned. 
                {
                    instReader.BaseStream.Position = BaseAddress + inst_offset; // Seek to the offset of the instrument.  
                    current_header = instReader.ReadUInt32(); // Read the 4 byte identity of the instrument.
                    var NewINST = new Instrument();
                    NewINST.Keys = new InstrumentKey[0xF0];
                    switch (current_header)
                    {
                        case INST:
                            {
                                instReader.ReadUInt32(); // The first 4 bytes following an instrument is always 0, for some reason.  Maybe reserved. 
                                NewINST.Pitch = instReader.ReadSingle(); // 4 byte float pitch
                                NewINST.Volume = instReader.ReadSingle(); // 4 byte float volume
                                /* Lots of skipping, i havent added these yet, but i'll comment what they are. */
                                instReader.ReadUInt32(); // offset to first oscillator table
                                instReader.ReadUInt32(); // Offset to second oscillator count
                                instReader.ReadUInt32(); // Offset to first effect object
                                instReader.ReadUInt32(); // offset to second effect object
                                instReader.ReadUInt32(); // offset of first sensor object
                                instReader.ReadUInt32(); // offset of second sensor object
                                /*////////////////////////////////////////////////////////////////////////////*/
                                var keyCounts = instReader.ReadInt32(); // How many key regions are in here.
                                int KeyHigh = 0;
                                int KeyLow = 0;
                                for (int k = 0; k < keyCounts; k++)
                                {
                                    var NewKey = new InstrumentKey();
                                    NewKey.keys = new InstrumentKeyVelocity[0x81];
                                    var keyreg_offset = instReader.ReadInt32(); // This will be where the data for our key region is. 
                                    var keyptr_return = instReader.BaseStream.Position; // This is our position after reading the pointer, we'll need to return to it
                                    instReader.BaseStream.Position = BaseAddress + keyreg_offset;  // Seek to the key region 
                                    byte key = instReader.ReadByte(); // Read the key identifierr
                                    KeyHigh = key;  // Set the highest key to what we just read. 
                                    instReader.BaseStream.Seek(3, SeekOrigin.Current); // 3 bytes, unused.
                                    var VelocityRegionCount = instReader.ReadInt32(); // read the number of entries in the velocity region array
                                    for (int b = 0; b < VelocityRegionCount; b++)
                                    {
                                        var NewVelR = new InstrumentKeyVelocity();
                                        var velreg_offs = instReader.ReadInt32(); // read the offset of the velocity region
                                        var velreg_retn = instReader.BaseStream.Position;  // another one of these.  Return pointer
                                        instReader.BaseStream.Position = velreg_offs + BaseAddress;
                                        int VelLow = 0;
                                        int VelHigh = 0;
                                        {
                                            var velocity = instReader.ReadByte(); // The velocity of this key.
                                            VelHigh = velocity;
                                            instReader.BaseStream.Seek(3, SeekOrigin.Current); // Unused.
                                            NewVelR.velocity = velocity;
                                            NewVelR.wsysid = instReader.ReadUInt16(); // This will be the ID of the WAVESYSTEM that its in
                                            NewVelR.wave = instReader.ReadUInt16(); // This will be the ID of the wave inside of that wavesystem
                                            NewVelR.Volume = instReader.ReadSingle(); // Finetune, volume, float
                                            NewVelR.Pitch = instReader.ReadSingle(); // finetune pitch, float. 
                                            for (int idx = 0; idx < (VelHigh - VelLow); idx++) // See below for what this is doing
                                            {
                                                NewKey.keys[(VelLow + idx)] = NewVelR;
                                            }
                                            VelLow = VelHigh;
                                        }
                                        instReader.BaseStream.Position = velreg_retn; // return to our pointer position  [THIS IS BELOW]
                                    }
                                    for (int idx = 0; idx < (KeyHigh - KeyLow); idx++) // The keys are gappy.
                                    {
                                        NewINST.Keys[(KeyLow + idx)] = NewKey; // So we want to interpolate the previous keys across the empty ones, so that way it's a region
                                    }
                                    KeyLow = KeyHigh; // Set our new lowest key to the previous highest
                                    instReader.BaseStream.Position = keyptr_return; // return to our last pointer position
                                }


                                break;
                            }
                        case PER2:

                            break;

                        case PERC:

                            break;
                    }
                    Instruments[inst_id] = NewINST; // Store it in the instruments bank
                }
                instReader.BaseStream.Position = anchor; // return back to our original pos to read the next pointer             
            }

        
    }

    }
}
