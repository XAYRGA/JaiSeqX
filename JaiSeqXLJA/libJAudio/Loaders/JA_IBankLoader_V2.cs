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
        private const int PMAP = 0x504D4150;
        private const int Pmap = 0x506D6170;
        private const int LIST = 0x4C495354;

        private int iBase = 0;
        private int OscTableOffset = 0;
        private int EnvTableOffset = 0;
        private int RanTableOffset = 0;
        private int SenTableOffset = 0;
        private int ListTableOffset = 0;
        private int PmapTableOffset = 0; 
        

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
            Console.WriteLine(Base);
            iBase = Base;
            if (binStream.ReadInt32() != IBNK)
                throw new InvalidDataException("Section doesn't have an IBNK header");
            Boundaries = binStream.ReadInt32() + 8; // total length of our section, the data of the section starts at +8, so we need to account for that, too.
            RetIBNK.id = binStream.ReadInt32(); // Forgot this. Ibank ID. Important.
            OscTableOffset = findChunk(binStream, OSCT); // Load oscillator table chunk
            EnvTableOffset = findChunk(binStream, ENVT); // Load envelope table chunk
            RanTableOffset = findChunk(binStream, RAND); // Load random effect table chunk
            SenTableOffset = findChunk(binStream, SENS); // Load sensor table chunk
            ListTableOffset = findChunk(binStream, LIST); // load the istrument list
            PmapTableOffset = findChunk(binStream, PMAP);  // Percussion mapping lookup table

            binStream.BaseStream.Position = OscTableOffset + iBase; // Seek to the position of the oscillator table
            loadBankOscTable(binStream, Base); // Load oscillator table, also handles the ENVT!!
            binStream.BaseStream.Position = ListTableOffset + iBase; // Seek to the instrument list base
            var instruments = loadInstrumentList(binStream, Base); // Load it.
            RetIBNK.Instruments = instruments;
            return RetIBNK;
        }


        /*
            JAIV2 LIST STRUCTURE
            0x00 - int32 0x4C495354 'LIST';
            0x04 - int32 length 
            0x08 - int32 count 
            0x0c - int32[count] instrumentPointers (RELATIVE TO IBANK 0x00)
        */

        public JInstrument[] loadInstrumentList(BeBinaryReader binStream, int Base)
        {
            JInstrument[] instruments = new JInstrument[0xF0]; // JSystem doesn't have more than 0xF0 instruments in each bank
            if (binStream.ReadInt32() != LIST) // Verify we're loading the right section
                throw new InvalidDataException("LIST data section started with unexpected data " + binStream.BaseStream.Position); // Throw if it's not the right data
            binStream.ReadInt32(); // Section Length // Section lenght doesn't matter, but we have to read it to keep alignment.
            var count = binStream.ReadInt32(); // Count of entries in the section (Including nulls.)
            // why are these FUCKS relative whenever literally nothing else in the file is ? //
            var pointers = Helpers.readInt32Array(binStream, count); // This will be an in32[] of pointers

            for (int i = 0; i < count; i++)
            {

                if (pointers[i] < 1) // Instrument is empty.
                    continue; // Instrument is empty. Skip this iteration
                binStream.BaseStream.Position = Base + pointers[i]; // FUCK THIS. Err I mean. Seek to the position of the instrument index + the base of the bank.
                var IID = binStream.ReadInt32();  // read the identity at the base of each section
                binStream.BaseStream.Seek(-4, SeekOrigin.Current); // Seek back identity (We read 4 bytes for the ID)

                switch (IID) // Switch ID
                {
                    case Inst: // It's a regular instrument 
                        instruments[i] = loadInstrument(binStream, Base); // Ask it to load (We're already just behind the Inst)
                        break;
                    case Perc: // Percussion Instrument 
                        instruments[i] = loadPercussionInstrument(binStream, Base);
                        break;
                    default:

                        Console.WriteLine("unknown inst index {0:X}" , binStream.BaseStream.Position);
                        break;
                }
            }

            return instruments;
        }

        /* JAIV2 Instrument Structure 
              0x00 int32 = 0x496E7374 'Inst'
              0x04 int32 oscillatorCount
              0x08 int32[oscillatorCount] oscillatorIndicies
              ???? int32 0 
              ???? int32 keyRegionCount 
              ???? keyRegion[keyRegionCount]
              ???? float gain
              ???? float freqmultiplier
             
        */
        public JInstrument loadInstrument(BeBinaryReader binStream, int Base)
        {
            var newInst = new JInstrument();
            newInst.IsPercussion = false; // Instrument isn't percussion
            // This is wrong, they come at the end of the instrument
            //newInst.Pitch = 1; // So these kinds of instruments don't initialize with a pitch value, which is strange. 
            //newInst.Volume = 1; // I guess they figured that it was redundant since they're already doing it in 3 other places. 
            if (binStream.ReadInt32() != Inst)
                throw new InvalidDataException("Inst section started with unexpected data");
            var osciCount = binStream.ReadInt32(); // Read the count of the oscillators. 
            newInst.oscillatorCount = (byte)osciCount; // Hope no instrument never ever ever has > 255 oscillators lol.
            newInst.oscillators = new JOscillator[osciCount]; // Initialize the instrument with the proper amount of oscillaotrs
            for (int i =0; i < osciCount; i++) // Loop through and read each oscillator.
            {
                var osciIndex = binStream.ReadInt32(); // Each oscillator is stored as a 32 bit index.
                newInst.oscillators[i] = bankOscillators[osciIndex]; // We loaded the oscillators already, I hope.  So this will grab them by their index.                
            }
            var notpadding = binStream.ReadInt32(); // NOT PADDING. FUCK. Probably effects.
            Helpers.readInt32Array(binStream, notpadding);
            var keyRegCount = binStream.ReadInt32();
            var keyLow = 0; // For region spanning. 
            JInstrumentKey[] keys = new JInstrumentKey[0x81]; // Always go for one more.
            for (int i = 0; i < keyRegCount; i++) // Loop through all pointers.
            {
                var bkey = readKeyRegion(binStream, Base); // Read the key region
                //Console.WriteLine("KREG BASE KEY {0}", bkey.baseKey);
                for (int b = 0; b < bkey.baseKey - keyLow; b++)
                {
                    //  They're key regions, so we're going to have a toothy / gappy piano. So we need to span the missing gaps with the previous region config.
                    // This means that if the last key was 4, and the next key was 8 -- whatever parameters 4 had will span keys 4 5 6 and 7. 
                    keys[b + keyLow] = bkey; // span the keys
                    keys[127] = bkey;
                }
                keyLow = bkey.baseKey; // Store our last key 
            }
            newInst.Keys = keys;
         
            newInst.Volume = binStream.ReadSingle(); // ^^
            newInst.Pitch = binStream.ReadSingle(); // Pitch and volume come last???
            // WE HAVE READ EVERY BYTE IN THE INST, WOOO
            return newInst;
        }


        public JInstrument loadPercussionInstrument(BeBinaryReader binStream, int Base)
        {
            if (binStream.ReadInt32() != Perc)
                throw new InvalidDataException("Perc section started with unexpected data");
            var newPERC = new JInstrument();
            newPERC.IsPercussion = true;
            newPERC.Pitch = 1f;
            newPERC.Volume = 1f;
    
            var count = binStream.ReadInt32();
            var ptrs = Helpers.readInt32Array(binStream, count);
            var iKeys = new JInstrumentKey[count];
            for (int i = 0; i < count; i++)
            {
                var PmapOffset = ptrs[i];
                if (PmapOffset > 0)
                {
                    var newKey = new JInstrumentKey();
                    newKey.Velocities = new JInstrumentKeyVelocity[0x81];
                    var pmapDataOffs = PmapOffset + Base ; // OH LOOK ANOTHER RELATIVE TO BANK BASE.
                    binStream.BaseStream.Position = pmapDataOffs;
                    if (binStream.ReadInt32() != Pmap)
                    {
                        Console.WriteLine("ERROR: Invalid PMAP data {0:X} -- Potential misalignment!", binStream.BaseStream.Position);
                        continue;
                    }
      
                    newKey.Volume = binStream.ReadInt32();
                    newKey.Pitch = binStream.ReadInt32();
                    //binStream.ReadInt32(); // byte panning 
                    binStream.BaseStream.Seek(8, SeekOrigin.Current); // runtime. 
                    var velRegCount = binStream.ReadInt32();
                    var velLow = 0;
                    for (int b = 0; b < velRegCount; b++)
                    {
                        var breg = readKeyVelRegion(binStream, Base);
                        for (int c = 0; c < breg.baseVel - velLow; c++)
                        {
                            //  They're velocity regions, so we're going to have a toothy / gappy piano. So we need to span the missing gaps with the previous region config.
                            // This means that if the last key was 4, and the next key was 8 -- whatever parameters 4 had will span keys 4 5 6 and 7. 
                            newKey.Velocities[c] = breg; // store the  region
                            newKey.Velocities[127] = breg;
                        }
                        velLow = breg.baseVel; // store the velocity for spanning
                    }
                    iKeys[i] = newKey;
                }
            }
            newPERC.Keys = iKeys;
            newPERC.oscillatorCount = 0; 
            return newPERC;
        }


        /* 
          JAIV2 KeyRegion Structure
          0x00 byte baseKey 
          0x01 byte[0x3] unused;
          0x04 int32 velocityRegionCount
          VelocityRegion[velocityRegionCount] velocities; // NOTE THESE ARENT POINTERS, THESE ARE ACTUAL OBJECTS.
        */

        public JInstrumentKey readKeyRegion(BeBinaryReader binStream, int Base)
        {
            JInstrumentKey newKey = new JInstrumentKey();
            newKey.Velocities = new JInstrumentKeyVelocity[0x81]; // Create region array
            //-------
            //Console.WriteLine(binStream.BaseStream.Position);
            newKey.baseKey = binStream.ReadByte(); // Store base key
            binStream.BaseStream.Seek(3, SeekOrigin.Current); ; // Skip 3 bytes
            var velRegCount = binStream.ReadInt32(); // Grab vel region count
            var velLow = 0;  // Again, these are regions -- see LoadInstrument for this exact code ( a few lines above ) 
            for (int i = 0; i < velRegCount; i++)
            {
                var breg = readKeyVelRegion(binStream, Base);  // Read the vel region.

                for (int b = 0; b < breg.baseVel - velLow; b++)
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
         JAIV2 Velocity Region Structure
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


        /* JAIV1 OSCT Structure
            0x00 int32 0x4F534354 'OSCT'
            0x04 int32 SectionLength (+8 for entire section)
            0x08 int32 OscillatorCouint       
        */

        private void loadBankOscTable(BeBinaryReader binStream, int Base)
        {
            if (binStream.ReadInt32() != OSCT) // Check if it has the oscillator table header
                throw new InvalidDataException("Oscillator table section started with unexpected data " + binStream.BaseStream.Position ); // Throw if it doesn't
            binStream.ReadInt32(); // This is the section length, its mainly used for seeking the file so we won't touch it.
            var count = binStream.ReadInt32(); // Read the count
            bankOscillators = new JOscillator[count]; // Initialize the bank oscillators with the number of oscillators int he table
            for (int i = 0; i < count; i++) // Loop through each onne
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
        public JEnvelope readEnvelope(BeBinaryReader binStream, int Base)
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
            JEnvelopeVector[] OscVecs = new JEnvelopeVector[len]; // And create an array the size of our length.

            for (int i = 0; i < len - 1; i++) // we read - 1 because we don't want to read the end value yet
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
            OscVecs[OscVecs.Length - 1] = new JEnvelopeVector
            {
                mode = (JEnvelopeVectorMode)binStream.ReadInt16(), // Read the values of each into their places
                time = (short)(lastVector.time), // read time 
                value = lastVector.value // read value
            };
            // Setting up references. 
            // can only be done after sorting :v...
            for (int idx = 0; idx < OscVecs.Length - 1; idx++)
            {
                OscVecs[idx].next = OscVecs[idx + 1]; // current vector objects next is the one after it.
            }
            var ret = new JEnvelope();
            ret.vectorList = OscVecs;
            return ret; // finally, return. 
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
          JAIV2 Oscillator Structure
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
                Osc.envelopes[0] = readEnvelope(binStream, EnvTableBase + 8); // Load the table
            }
            if (releaseDecayTableOffset > 0) // Next is RD table
            {
                binStream.BaseStream.Position = releaseDecayTableOffset + EnvTableBase + 8; // Seek to the vector and load it
                Osc.envelopes[1] = readEnvelope(binStream, EnvTableBase + 8); // loadddd
            }
            Osc.target = OscillatorTargetConversionLUT[target];
            return Osc;
        }
    }
}
