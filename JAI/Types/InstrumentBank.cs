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
        private const uint Inst = 0x496E7374;

        public void LoadInstrumentBank(BeBinaryReader instReader, JAIVersion version)
        {

            Instruments = new Instrument[0xF0]; // for some reason, they will only ever have 0xF0 instruments in them
          
            if (version==JAIVersion.ONE || version==JAIVersion.TWO)
            {
                loadIBNKJaiV1(instReader); // Both of these use standard ibnk structure. 
            } else if (version==JAIVersion.THREE)
            {
                loadIBNKJaiV2(instReader);
            }      
        }


        private long ReadJARCSizePointer(BeBinaryReader br)
        {
            var sectSize = br.ReadUInt32();
            return sectSize + 8;  // This is basically taking the section size pointer and adding 8 to it, because the size is always 8 bytes deep. 
            // Adding 8 to this makes it a pointer to the next section relative to the section base :v
        }


        private void loadIBNKJaiV2(BeBinaryReader instReader) 
        {
            long anchor = 0;
            var BaseAddress = instReader.BaseStream.Position;
            var current_header = instReader.ReadUInt32();
            if (current_header != 0x49424e4b) // Check to see if it equals IBNK
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("0x{0:X}", BaseAddress);
                throw new InvalidDataException("Scanned header is not an IBNK.");
            }

            var IBNKSize = instReader.ReadUInt32();
            id = instReader.ReadInt32();
            var flags = instReader.ReadUInt32(); // usually 1, determines if the bank is melodic or used for sound effects. Usually the bank is melodic. 
            instReader.BaseStream.Seek(0x10, SeekOrigin.Current); // Skip 16 bytes, always 0x00, please document if wrong. 
            var i = 0;
            while (true) {
                anchor = instReader.BaseStream.Position; // Store current section base. 
                i++;
               // Console.WriteLine("Iteration {0} 0x{1:X}", i,anchor);
                current_header = instReader.ReadUInt32();
              //  Console.WriteLine("GOT HD {0:X}", current_header);
                if (current_header==0x00)
                {
                    break;  // End of section. 
                } else if (current_header < 0xFFFF) // Read below for explanation
                {
                    // I've noticed VERY RARELY, that the section pointer is wrong by two bytes. This sucks. So we check if it's less than two full bytes.
                    // If it is, we seek back 2 bytes then read again, and our alignment is fixed :). 
                    // of course, this comes after our check to see if it's 0, which indicates end of section
                    instReader.BaseStream.Seek(-2, SeekOrigin.Current);
                    Console.WriteLine("[!] Misalignment detected in IBNK, new position 0x{0:X}",instReader.BaseStream.Position);
                    anchor = instReader.BaseStream.Position; // 3/14/2019, i forgot this. It's S M R T to update your read base. 
                    current_header = instReader.ReadUInt32();
                    Console.WriteLine("New header {0:X}", current_header);
                }
                var next_section = ReadJARCSizePointer(instReader); // if that works, go ahead and grab the 'pointer' to the next section. 
                if (current_header < 0xFFFF)
                {
                    Console.WriteLine("Corrupt IBNK 0x{0:X}", BaseAddress);
                    break;
                }
                switch (current_header)
                {
                    case 0x4F534354: // OSCT
                    case 0x52414E44: // RAND 
                    case 0x454E5654: // EVNT
                    case 0x53454E53: // SENS 
                        instReader.BaseStream.Position = anchor + next_section; // Skip section. 
                        break;
                    case INST:
                        {
                            var NewINST = new Instrument();
                            var InstCount = instReader.ReadInt32();
                            NewINST.Keys = new InstrumentKey[0xF0];
                            for (int instid = 0; instid < InstCount; instid++)
                            {
                                current_header = instReader.ReadUInt32();
                                var iebase = instReader.BaseStream.Position;
                                if (current_header != Inst)
                                {
                                    break; // FUCK. 
                                }
                                NewINST.oscillator = instReader.ReadInt32();
                                NewINST.id = instReader.ReadInt32();

                                instReader.ReadInt32(); // cant figure out what these are. 
                                var keyCounts = instReader.ReadInt32(); // How many key regions are in here.
                                int KeyHigh = 0;
                                int KeyLow = 0;
                                for (int k = 0; k < keyCounts; k++)
                                {
                                    var NewKey = new InstrumentKey();
                                    NewKey.keys = new InstrumentKeyVelocity[0x81];
                    
                                    byte key = instReader.ReadByte(); // Read the key identifierr
                                    KeyHigh = key;  // Set the highest key to what we just read. 
                                    instReader.BaseStream.Seek(3, SeekOrigin.Current); // 3 bytes, unused.
                                    var VelocityRegionCount = instReader.ReadInt32(); // read the number of entries in the velocity region array\
                                    if (VelocityRegionCount > 0x7F)
                                    {

                                        Console.WriteLine("Alignment is fucked, IBNK load aborted.");
                                        Console.WriteLine("E: VelocityRegionCount is too thicc. {0} > 128", VelocityRegionCount);
                                        Console.WriteLine("IBASE: 0x{0:X} + 0x{1:X} ({2:X})", anchor, iebase - anchor, (instReader.BaseStream.Position - anchor) - (iebase - anchor)  );
                                        
                                        return;
                                    }
                                    for (int b = 0; b < VelocityRegionCount; b++)
                                    {
                                        var NewVelR = new InstrumentKeyVelocity();
                                        int VelLow = 0;
                                        int VelHigh = 0;
                                        {
                                            var velocity = instReader.ReadByte(); // The velocity of this key.
                                            VelHigh = velocity;
                                            instReader.BaseStream.Seek(3, SeekOrigin.Current); // Unused.
                                            NewVelR.velocity = velocity;
                                            NewVelR.wave = instReader.ReadUInt16(); // This will be the ID of the wave inside of that wavesystem
                                            NewVelR.wsysid = instReader.ReadUInt16(); // This will be the ID of the WAVESYSTEM that its in
                                       
                                            NewVelR.Volume = instReader.ReadSingle(); // Finetune, volume, float
                                            NewVelR.Pitch = instReader.ReadSingle(); // finetune pitch, float. 
                                            for (int idx = 0; idx < (VelHigh - VelLow); idx++) // See below for what this is doing
                                            {
                                                NewKey.keys[(VelLow + idx)] = NewVelR;
                                                NewKey.keys[127] = NewVelR;
                                            }
                                            VelLow = VelHigh;
                                        }
              
                                    }
                                    for (int idx = 0; idx < (KeyHigh - KeyLow); idx++) // The keys are gappy.
                                    {
                                        NewINST.Keys[(KeyLow + idx)] = NewKey; // So we want to interpolate the previous keys across the empty ones, so that way it's a region
                                        NewINST.Keys[127] = NewKey;
                                    }
                                    KeyLow = KeyHigh; // Set our new lowest key to the previous highest
                                }
                            }
                            instReader.BaseStream.Position = anchor + next_section; // SAFETY.
                            break;
                        }
                    case PERC:

                        instReader.BaseStream.Position = anchor + next_section;
                        break;
                    case 0x4C495354: // LIST 
                        instReader.BaseStream.Position = anchor + next_section; // Skip section 
                        // Explanation: This is just a set of pointers relative to BaseAddress for the instruments, nothing special because we're
                        // already parsing them above. 
                        break;
                    default:
                        instReader.BaseStream.Position = anchor + next_section; // Skip section. 
                        break; 
                }
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
                                var poscioffs = instReader.ReadUInt32(); // offset to first oscillator table
                                var poscicnt = instReader.ReadUInt32(); // Offset to second oscillator count
                                //Console.WriteLine("Oscillator at 0x{0:X}, length {1}", poscioffs, poscicnt);
                                //Console.ReadLine();
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
                                            for (int idx = 0; idx < ( (1+ VelHigh) - VelLow); idx++) // See below for what this is doing
                                            {
                                                NewKey.keys[(VelLow + idx)] = NewVelR;
                                                NewKey.keys[127] = NewVelR;
                                            }
                                            VelLow = VelHigh;
                                        }
                                        instReader.BaseStream.Position = velreg_retn; // return to our pointer position  [THIS IS BELOW]
                                    }
                                    for (int idx = 0; idx < (KeyHigh - KeyLow); idx++) // The keys are gappy.
                                    {
                                        NewINST.Keys[(KeyLow + idx)] = NewKey; // So we want to interpolate the previous keys across the empty ones, so that way it's a region
                                        NewINST.Keys[127] = NewKey;
                                    }
                                    KeyLow = KeyHigh; // Set our new lowest key to the previous highest
                                    instReader.BaseStream.Position = keyptr_return; // return to our last pointer position
                                }


                                break;
                            }
                        case PER2:
                            {
                                NewINST.IsPercussion = true;
                                instReader.BaseStream.Seek(0x84, SeekOrigin.Current); // 0x88 - 4 (PERC) 
                                for (int per = 0; per < 100; per++)
                                {
                                    var NewKey = new InstrumentKey();
                                    NewKey.keys = new InstrumentKeyVelocity[0x81];

                                    var keyreg_offset = instReader.ReadInt32(); // This will be where the data for our key region is. 
                                    var keyptr_return = instReader.BaseStream.Position; // This is our position after reading the pointer, we'll need to return to it
                                    if (keyreg_offset == 0)
                                    {
                                        continue; // Skip, its empty. 
                                    }
                                    instReader.BaseStream.Position = BaseAddress + keyreg_offset; // seek to position. 
                                    NewINST.Pitch = instReader.ReadSingle();
                                    NewINST.Volume = instReader.ReadSingle();
                                    instReader.BaseStream.Seek(8, SeekOrigin.Current);
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
                                            for (int idx = 0; idx < (VelHigh - (VelLow )); idx++) // See below for what this is doing
                                            {
                                                NewKey.keys[(VelLow + (idx))] = NewVelR;
                                                NewKey.keys[127] = NewVelR;
                                            }
                                            VelLow = VelHigh;
                                        }
                                        instReader.BaseStream.Position = velreg_retn; // return to our pointer position  [THIS IS BELOW]
                                    }
                                    instReader.BaseStream.Position = keyptr_return;
                                    NewINST.Keys[per] = NewKey; // oops, add to instrument data or else it doesnt load x.x
                                    NewINST.Keys[127] = NewKey;
                                }

                                break;
                            }
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
