using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Be.IO;

namespace libJAudio.Loaders
{
    internal class  C_DFEntry // Helper class for  returning all data when reading each entry inside of the C_DF.
    {
        public short awid;
        public short waveid;
    }

    public class JA_WSYSLoader_V1
    {


        /* STRUCTURE OF A WSYS */
        /* ******************************
         * ** NO POINTERS ARE ABSOLUTE **
         * ** ALL ARE RELATIVE TO WSYS **
         * ******************************
         * 
            0x00 - int32 0x57535953  ('WSYS')
            0x04 - int32 size
            0x08 - int32 global_id
            0x0C - int32 unused 
            0x10 - int32 Offset to WINF
            0x14 - int32 Offset to WBCT
            
            STRUCTURE OF WINF
            0x00 - int32 ? ('WINF')
            0x04 - int32 count // SHOULD ALWAYS BE THE SAME  COUNT AS THE WBCT.
            0x08 - *int32[count] (WaveGroup)Pointers

            STRUCTURE OF WBCT  
            0x00 - int32 ('WBCT')
            0x04 - int32 unknown
            0x08 - int32 count // SHOULD ALWAYS BE THE SAME COUNT AS THE WINF. 
            0x0C - *int32[count] (SCNE)Pointers

            STRUCTURE OF SCNE
            0x00 - int32 ('SCNE')
            0x04 - long 0;
            0x0C - pointer to C-DF
            0x10 - pointer to C-EX // Goes completely unused?
            0x14 - pointer to C-ST // Goes completely unused? 

            STRUCTURE OF C_DF 
            0x00 - int32 ('C-DF')
            0x04 - int32 count
            0x08 - *int32[count] (waveID)Pointers

            STRUCTURE OF (waveID) 
            0x02 - short awID
            0x04 - short waveID
            
            
            STRUCTURE OF 'WaveGroup'  
            0x00 - byte[0x70] ArchiveName
            0x70 - int32 waveCount 
            0x74 - *int32[waveCount] (wave)Pointers
            
            STRUCTURE OF 'wave'
            0x00 - byte unknown
            0x01 - byte format
            0x02 - byte baseKey  
            0x03 - byte unknown 
            0x04 - float sampleRate 
            0x08 - int32 start
            0x0C - int32 length 
            0x10 - int32 loop >  0  ?  true : false 
            0x14 - int32 loop_start
            0x18 - int32 loop_end 
            0x1C  - int32 sampleCount 


            -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
             ~~ IMPORTANT NOTES ABOUT SCNE AND WAVEGROUPS ~~
             The SCNE and WaveGroups are parallel. 
             this means, that SCNE[1] pairs with WaveGroup[1].
             So, when  you load the waves from the WaveGroup, 
             the first entry in the SCNE's C-DF matches
             with the first wave in the WaveGroup. 
             This means: 
             SCNE.C-DF[1] matches with WaveGroup.Waves[1]. 
             Also indicating,  that the first Wave-ID in the 
             C-DF matches with the first wave in the WaveGroup


        */

        private const int WSYS = 0x57535953;
        private const int SCNE = 0x53434E45;
        private const int C_DF = 0x432D4446;

        public int[] readWINF(BeBinaryReader binSteam, int Base)
        {
            var HEAD = binSteam.ReadInt32(); // Read the header (WINF)
            var LENGTH = binSteam.ReadInt32(); // Read the count of how many pointers we have
            return Helpers.readInt32Array(binSteam, LENGTH); // Read all of the pointers, and return.
        }

        public int[] readWBCT(BeBinaryReader binSteam, int Base)
        {
            var HEAD = binSteam.ReadInt32(); // Read the header 
            var unk = binSteam.ReadInt32(); // Read unknown 4 bytes
            var LENGTH = binSteam.ReadInt32(); // Read the count of how mamny pointers we have
            return Helpers.readInt32Array(binSteam, LENGTH); // Read all of the pointers, and return. 

        }


        public string readArchiveName(BinaryReader aafRead)
        {
            var ofs = aafRead.BaseStream.Position; // Store where we started 
            byte nextbyte; // Blank byte
            byte[] name = new byte[0x70]; // Array for the name

            int count = 0; // How many we've done
            while ((nextbyte = aafRead.ReadByte()) != 0xFF & nextbyte != 0x00) // Read until we've read 0 or FF
            {
                name[count] = nextbyte; // Store into byte array
                count++; // Count  how many valid bytes  we've read.
            }
            aafRead.BaseStream.Seek(ofs + 0x70, SeekOrigin.Begin); // Seek 0x70 bytes, because thats the statically allocated space for the wavegroup path. 
            return Encoding.ASCII.GetString(name, 0, count); // Return a string with the name, but only of the valid bytes we've read. 
        }


        public JWaveSystem loadWSYS(BeBinaryReader binStream, int Base)
        {
            var newWSYS = new JWaveSystem();
            binStream.BaseStream.Position = Base;
            if (binStream.ReadInt32() != WSYS) // Check if first 4 bytes are WSYS
                throw new InvalidDataException("Data is not an WSYS");
            var wsysSize = binStream.ReadInt32(); // Read the size of the WSYS
            var wsysID = binStream.ReadInt32(); // Read WSYS ID
            var unk1 = binStream.ReadInt32(); // Unused?
            var waveINF = binStream.ReadInt32(); // Offset to  WINF
            var waveBCT = binStream.ReadInt32(); // Offset to WBCT
            /* Should probably squish this into a different function. And I did. */
            binStream.BaseStream.Position = waveINF + Base; // Seek to  WINF relative to base.  
            var winfoPointers = readWINF(binStream, Base); // Read the waveGroup pointers
            binStream.BaseStream.Position = waveBCT + Base; // Seek to the WBCT
            var wbctPointers = readWBCT(binStream, Base); // load the pointers for the wbct (Wave ID's)
            JWaveGroup[] WSYSGroups = new JWaveGroup[winfoPointers.Length]; // The count of waveInfo's determines the amount of groups -- there is one WINF entry per group. 
            newWSYS.WaveTable = new Dictionary<int, JWave>();

            for (int i=0; i <  WSYSGroups.Length; i++)
            {
                binStream.BaseStream.Position = Base + winfoPointers[i]; // Seek to the wavegroup base.
                WSYSGroups[i] = readWaveGroup(binStream, Base); // load the WaveGroup
            }
            for (int i=0; i < WSYSGroups.Length; i++)
            {
                var currentWG = WSYSGroups[i]; // After they've been loaded, we need to load their wave ID's
                binStream.BaseStream.Position = Base + wbctPointers[i]; // this is achieve by the WBCT, which points to a SCNE
                var scenes = loadScene(binStream, Base); // Load the SCNE object
                {
                    binStream.BaseStream.Position = scenes[0] + Base;  // The SCNE contains pointers to C-DF, C-EX, and C-ST -- we only know that C-DF works.  
                    var IDMap = loadC_DF(binStream, Base); // load the C_DF, which gives us  an array of C_DF entries, containing awID and WaveID. 
                    for (int b=0; b < IDMap.Length;b++)  // We need to loop over the map of ID's
                    {
                        currentWG.Waves[b].id = IDMap[b].waveid; // SCNE and WaveGroup are  1 to 1, meanin the first entry in one lines up with the other. 
                        // So we'll want to move the waveid into the wave object itself for convience. 
                        currentWG.WaveByID[IDMap[b].waveid] = currentWG.Waves[b]; // Basically making a copy of the wave object, so  it can be found by its ID instead of entry index.
                        newWSYS.WaveTable[IDMap[b].waveid] = currentWG.Waves[b]; // TODO: Add Wavegroup.load for wsys, not all waves are loaded all the time.
                    }
                }
            }
            newWSYS.id = wsysID; // We loaded the ID from above, store it
            newWSYS.Groups = WSYSGroups; // We need to store the groups too, so let's do that.
            return newWSYS;
        }

        private JWaveGroup readWaveGroup(BeBinaryReader binStream, int Base)
        {
            var newWG = new JWaveGroup(); // Create new Wavegroup object
            newWG.WaveByID = new Dictionary<int, JWave>(); // Initialize the dictionary (map) for the wave id's (used later)
            newWG.awFile = readArchiveName(binStream); // Exec helper functionn, which reads 0x70 bytes and trims off the fat. 
            var waveCount = binStream.ReadInt32(); // Read the wave count
            int[] WaveOffsets = Helpers.readInt32Array(binStream, waveCount); // Read waveCount int32's
            newWG.Waves = new JWave[waveCount]; // make an array with all of the wave counts, this is to store the waves we will read
            for (int i = 0; i <  waveCount; i++)
            {
                binStream.BaseStream.Position = Base + WaveOffsets[i]; // Seek to the offset of each eave
                newWG.Waves[i] = loadWave(binStream, Base); // Then tell it to load. 
                newWG.Waves[i].wsysFile = newWG.awFile;
            }
            return newWG;
        }


        private int[] loadScene(BeBinaryReader binStream, int Base)
        {
            binStream.ReadInt32();  // SCNE
            binStream.ReadUInt64(); // ? 8 empty bytes? Runtime,  maybe?
            return Helpers.readInt32Array(binStream, 3);  // C-DF, C-EX, C-ST
        }

        private C_DFEntry[] loadC_DF(BeBinaryReader binStream, int Base)
        {
            binStream.ReadInt32(); // should be C-DF.
            var count = binStream.ReadInt32(); // Read the count
            //Console.WriteLine("{0:X}" , binStream.BaseStream.Position); // DEBUG DEEEEBUG
            var Offsets = Helpers.readInt32Array(binStream, count); // Read all offsets, (count int32's)
            var idmap = new C_DFEntry[count]; // New array to store all the waveid's in
            for (int i=0; i <  count; i++)
            {
                binStream.BaseStream.Position = Offsets[i] + Base; // Seek to each c_DF entry
                idmap[i] = new C_DFEntry
                {
                    awid = binStream.ReadInt16(), // read AW ID
                    waveid = binStream.ReadInt16() // Read Wave ID
                };
            }

            return idmap;
        }

        private JWave loadWave(BeBinaryReader binStream,int Base)
        {
            var newWave = new JWave();
            binStream.ReadByte(); // First byte unknown?
            newWave.format = binStream.ReadByte(); // Read wave format, usually 5
            newWave.key = binStream.ReadByte(); // Read the base tuning key
            //Console.WriteLine(newWave.key);
            binStream.ReadByte(); // fourth byte unknown?
            newWave.sampleRate = binStream.ReadSingle(); // Read the samplerate
            newWave.wsys_start = binStream.ReadInt32(); // Read the offset in the AW
            newWave.wsys_size = binStream.ReadInt32(); // Read the length in the AW
            newWave.loop = binStream.ReadUInt32() == UInt32.MaxValue ? true : false; // Check if it loops?
            newWave.loop_start = binStream.ReadInt32(); // Even if looping is disabled, it should still read loops
            newWave.loop_end = binStream.ReadInt32(); // Sample index of loop end
            newWave.sampleCount = binStream.ReadInt32(); // Sample count
            return newWave;
        }
    }
}
