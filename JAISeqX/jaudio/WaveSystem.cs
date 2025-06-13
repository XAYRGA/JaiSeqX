/* WSYS Interface */
/* 2024 https://github.com/xayrga/jaudiostudio */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace jaudio
{
    public class JWaveSystem
    {
        public int ID;

        public Dictionary<int, Wave> Waves = new Dictionary<int, Wave>();
        public List<Scene> Scenes = new List<Scene>();

        public class Wave
        {
            public EWaveFormat Format;
            public EWaveFormat OriginalFormat;
            public bool NeedsReencoding = false;

            public byte Key = 60;
            public float SampleRate = 8000;
            public int SampleCount = 0;

            public bool Loop;
            public int LoopStart;
            public int LoopEnd;

            public int Last;
            public int Penult;

            public enum EWaveFormat
            {
                ADPCM4 = 0,
                ADPCM2 = 1,
                PCM8 = 2, 
                PCM16 = 3,
            }
        }
        
        public class Scene
        {
            public string AWName = "";
            public List<int> Waves = new List<int>();
        }
    }
}
