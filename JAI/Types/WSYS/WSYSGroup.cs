using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Be.IO;

namespace JaiSeqX.JAI.Types.WSYS
{
    public class WSYSGroup
    {
        private static int SequentialID; 

        public string path; // path to the .aw

        public BeBinaryReader handle; // file handle for the .aw 

        public int global_id;

        public int unique_id;

        public int[] IDMap;

        public WSYSWave[] Waves;

  
        public void UnpackSamples(ref WaveSystem root)
        {
            for (int i = 0; i < IDMap.Length; i++)
            {
                var index = IDMap[i];
                if (index > 0) {
                    
                    root.waves[index] = Waves[IDMap[index]];
                }
            }
        }


    }
}
