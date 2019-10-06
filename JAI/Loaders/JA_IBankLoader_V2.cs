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
    class JA_IBankLoader_V2
    {
        private const int PERC = 0x50455243; // Percussion Table
        private const int Perc = 0x50657263; // Percussion 
        private const int SENS = 0x53454E53; // Sensor effect
        private const int RAND = 0x52414E44; // Random Effect
        private const int OSCT = 0x4F534354; // OSCillator Table
        private const int Osci = 0x4F736369; // Oscillator
        private const int INST = 0x494E5354; // INStrument Table
        private const int Inst = 0x496E7374; // Instrument
        private const int IBNK = 0x49424E4B; // Instrument BaNK
        private const int ENVT = 0x454E5654; // ENVelope Table
        private const int LIST = 0x4C495354;

        private int iBase = 0;
        private int OscTableOffset = 0;
        private int EnvTableOffset = 0;
        private int RanTableOffset = 0;
        private int SenTableOffset = 0;
        private int ListTableOffset = 0;

        private int Boundaries = 0;

        private JOscillator[] bankOscillators;
        /* 
            NOTE ABOUT "OSCILLATORS"
            The envelope table must be loaded before the oscillator!
        */


        // [!] [!] [!] [!] [!] [!] [!] [!] [!] [!] [!] [!] [!] [!] //
        // [!]  THIS FUNCTION DESTROYS YOUR CURRENT POSITION   [!] //
        // [!] Remember to anchor before calling it or trouble [!] //
        // [!] [!] [!] [!] [!] [!] [!] [!] [!] [!] [!] [!] [!] [!] //
        private int findChunk(BeBinaryReader read, int chunkID, bool immediate = false) 
        {
            if (!immediate) // Indicating we need to search the entire bank (Default)
            {
                read.BaseStream.Position = iBase; // Seek back to IBNK, since i can't follow my own warnings. 
            }
            while (true)
            {
                var pos = (int)read.BaseStream.Position - iBase; // Store the position as an int, relative to ibase. 
                var i = read.ReadInt32(); // Read 4 bytes, since our chunkid is an int32
                if (i==chunkID) // Check to see if the chunk is what we're looking for
                {
                    Console.WriteLine("Found section {0:X}", chunkID);
                    return pos; // Return position relative to the base. 
                }
                else if (pos > (Boundaries)) // we exceedded boundaries
                {
                    Console.WriteLine("Failed to find section", chunkID);
                    return 0;
                }
            }
        }
        /* 
            ** CHUNKS DO NOT HAVE __ANY__ OFFSET POINTERS, LOCATION IS COMPLETELY VARIABLE ** 
            Chunks must be aligned multiple of 4 bytes of one another.
            JAIV2 IBNK Structure 
            ??? ENVT - Envelope Table 
            ??? OSCT - Oscillator Table 
            ??? RAND - Random Effects Table
            ??? SENS - Sensor Effect Table
            ??? INST - Instrument Table 
            ??? PMAP - Percussion Map
            ??? LIST - Instrument List 
        */
        public JIBank loadIBNK(BeBinaryReader binStream, int Base)
        {
            Console.WriteLine("Start load ibnk");
            var RetIBNK = new JIBank();
                                 
            iBase = Base;
            if (binStream.ReadInt32() != IBNK)
                throw new InvalidDataException("Section doesn't have an IBNK header");
            Boundaries = binStream.ReadInt32() + 8; // total length of our section, the data of the section starts at +8, so we need to account for that, too.
            OscTableOffset = findChunk(binStream, OSCT); 
            EnvTableOffset = findChunk(binStream, ENVT);
            RanTableOffset = findChunk(binStream, RAND);
            SenTableOffset = findChunk(binStream, SENS);
            ListTableOffset = findChunk(binStream, LIST);           

            binStream.BaseStream.Position = OscTableOffset + iBase;
            loadBankOscTable(binStream, Base); // Load oscillator table, also handles the ENVT. 
            binStream.BaseStream.Position = ListTableOffset + iBase;
            var instruments = loadInstrumentList(binStream, Base);

            return RetIBNK;
        }



        public JInstrument[] loadInstrumentList(BeBinaryReader binStream, int Base)
        {
            JInstrument[] instruments = new JInstrument[0xF0];
            if (binStream.ReadInt32() != LIST)
                throw new InvalidDataException("LIST data section started with unexpected data " + binStream.BaseStream.Position);
            binStream.ReadInt32(); // Section Length 
            var count = binStream.ReadInt32();
            // why are these FUCKS relative whenever literally nothing else in the file is ? //
            var pointers = Helpers.readInt32Array(binStream, count);

            for (int i = 0; i < count; i++)
            {
                binStream.BaseStream.Position = Base + pointers[i]; // FUCK THIS. Err I mean. Seek to the position of the instrument index.
                var IID = binStream.ReadInt32();  // read the identity at the base of each section
                binStream.BaseStream.Seek(-4, SeekOrigin.Current); // Seek back identity
                switch (IID)
                {
                    case Inst:

                        break;
                    case Perc:

                        break;
                }
            }

            return instruments;
        }


        /* JAIV1 OSCT Structure
            0x00 int32 0x4F534354 'OSCT'
            0x04 int32 SectionLength (+8 for entire section)
            0x08 int32 OscillatorCouint       
        */

        private void loadBankOscTable(BeBinaryReader binStream, int Base)
        {
            if (binStream.ReadInt32() != OSCT)
                throw new InvalidDataException("Oscillator table section started with unexpected data " + binStream.BaseStream.Position );
            binStream.ReadInt32(); // length 
            var count = binStream.ReadInt32(); // Read the count
            bankOscillators = new JOscillator[count];
            for (int i = 0; i < count; i++)
            {
                var returnPos = binStream.BaseStream.Position; // Save our position, the oscillator load function destroys our position.
                bankOscillators[i] = loadOscillator(binStream, EnvTableOffset + iBase); // Ask the oscillator to load
                binStream.BaseStream.Position = returnPos + 0x1c;// Oscillatrs are 0x1c in length, so advance to the next osc.
            }
        }

        /*
         * SAME AS JAIV1
        JAIV2 Oscillator Vector Structure 
            These are weird. so i'll just do it like this
            short mode
            short time
            short value

            when you read anything over 8 for the mode, read the last two shorts then stop reading -- so the last value in the array would be

            0x000F, 0x0000, 0x0000
        */
        public JOscillatorVector[] readOscVector(BeBinaryReader binStream, int Base)
        {
            var len = 0;
            // This function cheats a little bit :) 
            // We dive in not knowing the length of the table -- the table is stopped whenever one of the MODE bytes is more than 0xB. 
            var seekBase = binStream.BaseStream.Position;
            for (int i = 0; i < 10; i++)
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
            JOscillatorVector[] OscVecs = new JOscillatorVector[len]; // And create an array the size of our length.

            for (int i = 0; i < len - 1; i++) // we read - 1 because we don't want to read the end value yet
            {
                var vector = new JOscillatorVector
                {
                    mode = (JOscillatorVectorMode)binStream.ReadInt16(), // Read the values of each into their places
                    time = binStream.ReadInt16(), // read time 
                    value = binStream.ReadInt16() // read value
                };
                OscVecs[i] = vector;
            } // Go down below for the last vector, after sorting

            // todo: Figure out why this doesn't sort right? 

            // -1 is so we don't sort the last object in the array, because its null. We're sorting from the bottom up.
            for (int i = 0; i < len - 1; i++) // a third __fucking iteration__ on these stupid vectors.
            {
                for (int j = 0; j < len - 1; j++)
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
            OscVecs[OscVecs.Length - 1] = new JOscillatorVector
            {
                mode = (JOscillatorVectorMode)binStream.ReadInt16(), // Read the values of each into their places
                time = (short)(lastVector.time), // read time 
                value = lastVector.value // read value
            };

            return OscVecs; // finally, return. 
        }

        // We have to have this LUT for this function for JAIV2, because all the effect targets are shifted down one. 
        // Instead of rewriting the entire oscillator section for this, we can just make this LUT to convert them to the JAIV1 formats.
        // Really strange change.
        private JOscillatorTarget[] OscillatorTargetConversionLUT = new JOscillatorTarget[]
        {
            JOscillatorTarget.Volume, // 0 
            JOscillatorTarget.Pitch, // 1 
            JOscillatorTarget.Pan, // 2 
            JOscillatorTarget.FX, // 3
            JOscillatorTarget.Dolby, // 4
        };

        /*
          JAIV2 Oscillator Format 
          0x00 - int32 0x4F736369 'Osci'
          0x04 - byte mode 
          0x05 - byte[3] unknown
          0x08 - float rate
          0x0c - int32 attackVectorOffset (RELATIVE TO ENVT + 0x08)
          0x10 - int32 releaseVectorOffset (RELATIVE TO ENVT + 0x08)
          0x14 - float width
          0x18 - float vertex
       */

        /* NOTE THAT THESE OSCILLATORS HAVE THE SAME FORMAT AS JAIV1, HOWEVER THE VECTORS ARE IN THE ENVT */
        public JOscillator loadOscillator(BeBinaryReader binStream, int EnvTableBase)
        {
            var Osc = new JOscillator(); // Create new oscillator           
            if (binStream.ReadInt32() != Osci) // Read first 4 bytes
                throw new InvalidDataException("Oscillator format is invalid. " + binStream.BaseStream.Position);
      
            var target = binStream.ReadByte(); // load target -- what is it affecting?
            binStream.BaseStream.Seek(3, SeekOrigin.Current); // read 3 bytes?
            Osc.rate = binStream.ReadSingle(); // Read the rate at which the oscillator progresses -- this will be relative to the number of ticks per beat.
            var attackSustainTableOffset = binStream.ReadInt32(); // Offset of AD table
            var releaseDecayTableOffset = binStream.ReadInt32(); // Offset of SR table
            Osc.Width = binStream.ReadSingle(); // We should load these next, this is the width, ergo the value of the oscillator at 32768. 
            Osc.Vertex = binStream.ReadSingle();  // This is the vertex, the oscillator will always cross this point. 
            // To determine the value of an oscillator, it's Vertex + Width*(value/32768) -- each vector should progress the value, depending on the mode. 
            // We need to add + 8 to the offsets, because the pointers are the offset based on where the data starts, not the section
            if (attackSustainTableOffset > 0) // first is AS table
            {
                binStream.BaseStream.Position = attackSustainTableOffset + EnvTableBase + 8; // Seek to the vector table
                Osc.ASVector = readOscVector(binStream, EnvTableBase + 8); // Load the table
            }
            if (releaseDecayTableOffset > 0) // Next is RD table
            {
                binStream.BaseStream.Position = releaseDecayTableOffset + EnvTableBase + 8; // Seek to the vector and load it
                Osc.DRVector = readOscVector(binStream, EnvTableBase + 8); // loadddd
            }
            Osc.target = OscillatorTargetConversionLUT[target];
            return Osc;
        }


    }
}
