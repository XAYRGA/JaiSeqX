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
