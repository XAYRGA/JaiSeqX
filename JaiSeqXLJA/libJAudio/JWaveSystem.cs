using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libJAudio
{


    public class JWaveGroup
    {
        public string awFile;
        public JWave[] Waves;
        public Dictionary<int, JWave> WaveByID;
    }
    public class JWaveSystem
    {
        public int id;
        public JWaveGroup[] Groups;
        public JWave[] LoadedWaves;
        public Dictionary<int, JWave> WaveTable;
    }
}
