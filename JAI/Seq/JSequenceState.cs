using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JaiSeqX.JAI.Seq
{

    // Track state object, tells all of the parameters of the track at any given point. 
    public struct JSequenceState
    {
       
        public byte note; 
        public byte voice;
        public byte vel;
        
        public int delay;
        
        public byte param;
        public short param_value;

        public int perf;
        public int perf_value;
        public int perf_duration;
        
        public byte voice_bank;
        public byte voice_program;
        
        public short ppqn;
        public short bpm;
        
        public int jump_address;
        public byte jump_mode;

        public int track_id; 
        public int track_address; 

    }
}
