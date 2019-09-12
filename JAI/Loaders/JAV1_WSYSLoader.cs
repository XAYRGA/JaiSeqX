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
    internal class  C_DF
    {
        public short awid;
        public short waveid;
    }

    class JAV1_WSYSLoader
    {
        private const int WSYS = 0x57535953;
        private const int SCNE = 0x53434E45;
        private const int C_DF = 0x432D4446;

        public int[] readWINF(BeBinaryReader binSteam,int Base)
        {
            var HEAD = binSteam.ReadInt32();
            var LENGTH = binSteam.ReadInt32();                  
            return Helpers.readInt32Array(binSteam, LENGTH);

        }

        public int[] readWBCT(BeBinaryReader binSteam, int Base)
        {
            var HEAD = binSteam.ReadInt32();
            var unk = binSteam.ReadInt32();
            var LENGTH = binSteam.ReadInt32();      
            return Helpers.readInt32Array(binSteam, LENGTH);

        }


        /* STRUCTURE OF A WSYS */
        /*
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


        public JWaveSystem loadWSYS(BeBinaryReader binStream, int Base)
        {
            var newWSYS = new JWaveSystem();

            if (binStream.ReadInt32() != WSYS) // Check if first 4 bytes are IBNK
                throw new InvalidDataException("Data is not an WSYS");
            var wsysSize = binStream.ReadInt32();
            var wsysID = binStream.ReadInt32();
            var unk1 = binStream.ReadInt32();
            var waveINF = binStream.ReadInt32(); // Offset to  WINF
            var waveBCT = binStream.ReadInt32(); // Offset to wbct 
            /* Should probably squish this into a different function.*/
            binStream.BaseStream.Position = waveINF + Base;
            var winfoPointers = readWINF(binStream, Base); // load the pointers for the winf
            binStream.BaseStream.Position = waveBCT + Base;
            var wbctPointers = readWBCT(binStream, Base); // load the pointers for the wbct
            JWaveGroup[] WSYSGroups = new JWaveGroup[winfoPointers.Length]; // The count of waveInfo's determines the amount of groups -- there is one WINF entry per group. 

            for (int i=0; i <  WSYSGroups.Length; i++)
            {
                binStream.BaseStream.Position = Base + winfoPointers[i];
                WSYSGroups[i] = readWaveGroup(binStream, Base);
            }
            for (int i=0; i < WSYSGroups.Length; i++)
            {
                var currentWG = WSYSGroups[i];
                binStream.BaseStream.Position = Base + winfoPointers[i];
                var scenes = loadScene(binStream, Base);
                {
                    binStream.BaseStream.Position = scenes[0];  // oh boy.  
                    var IDMap = loadC_DF(binStream, Base);
                    for (int b=0; b < IDMap.Length;b++)
                    {
                        currentWG.Waves[i].id = IDMap[i].waveid;
                        currentWG.WaveByID[IDMap[i].waveid] = currentWG.Waves[i];
                    }
                }
            }
            newWSYS.id = wsysID;
            newWSYS.Groups = WSYSGroups;
            return newWSYS;
        }

        private JWaveGroup readWaveGroup(BeBinaryReader binStream, int Base)
        {
            var newWG = new JWaveGroup();
            newWG.awFile = Helpers.readArchiveName(binStream);
            var waveCount = binStream.ReadInt32();
            int[] WaveOffsets = Helpers.readInt32Array(binStream, waveCount);
            newWG.Waves = new JWave[waveCount];
            for (int i = 0; i <  waveCount; i++)
            {
                binStream.BaseStream.Position = Base + WaveOffsets[i];
                newWG.Waves[i] = loadWave(binStream, Base);
            }
            return newWG;
        }


        private int[] loadScene(BeBinaryReader binStream, int Base)
        {
            binStream.ReadInt32();  // SCNE
            binStream.ReadUInt64(); // ? 8 empty bytes? Runtime,  maybe?
            return Helpers.readInt32Array(binStream, 3);  // C-DF, C-EX, C-ST
        }

        private C_DF[] loadC_DF(BeBinaryReader binStream, int Base)
        {
            binStream.ReadInt32(); // should be C-DF.
            var count = binStream.ReadInt32();
            var Offsets = Helpers.readInt32Array(binStream, count);
            var idmap = new C_DF[count];
            for (int i=0; i <  count; i++)
            {
                binStream.BaseStream.Position = Offsets[i] + Base;
                idmap[i] = new C_DF
                {
                    awid = binStream.ReadInt16(),
                    waveid = binStream.ReadInt16()
                };
            }

            return idmap;
        }

        private JWave loadWave(BeBinaryReader binStream,int Base)
        {
            var newWave = new JWave();
            binStream.ReadByte(); // First byte unknown?
            newWave.format = binStream.ReadByte();
            newWave.key = binStream.ReadByte();
            //Console.WriteLine(newWave.key);
            binStream.ReadByte(); // fourth byte unknown?
            newWave.sampleRate = binStream.ReadSingle();
            newWave.wsys_start = binStream.ReadInt32();
            newWave.wsys_size = binStream.ReadInt32();
            newWave.loop = binStream.ReadUInt32() == UInt32.MaxValue ? true : false;
            newWave.loop_start = binStream.ReadInt32();
            newWave.loop_end = binStream.ReadInt32();
            newWave.sampleCount = binStream.ReadInt32();
            return newWave;
        }
    }
}
