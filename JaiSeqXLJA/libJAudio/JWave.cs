using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libJAudio
{
    public class JWave
    {
        public int id;
        public ushort format;
        public ushort key;
        public double sampleRate;
        public int sampleCount;

        public string wsysFile; 
        public int wsys_start;
        public int wsys_size;

        public bool loop;
        public int loop_start;
        public int loop_end;

        public string fsPath;

        public byte[] pcmData;

    }
}
