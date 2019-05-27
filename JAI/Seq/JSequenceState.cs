using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JaiSeqX.JAI.Seq
{

    // Track state object, tells all of the parameters of the track at any given point. 
    public class JSequenceState
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
        public byte perf_type;
        public double perf_decimal;

        
        public byte voice_bank;
        public byte voice_program;
        
        public short ppqn;
        public short bpm;
        
        public int jump_address;
        public byte jump_mode;

        public int track_id; 
        public int track_address;
        public int track_stack_depth;


        public int current_address;

        public int[] registers;

        public string message; 

        public JSequenceState()
        {
            registers = new int[80];
        }
       

    }
}
