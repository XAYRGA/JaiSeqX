using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JaiSeqX.JAI.Types.WSYS
{
    public class WSYSWave
    {
        public int id;
        public ushort format;
        public ushort key;
        public double sampleRate;
        public int sampleCount;


        public uint w_start;
        public uint w_size;

        public bool loop;
        public int loop_start;
        public int loop_end;


        public string pcmpath;

    }

}
