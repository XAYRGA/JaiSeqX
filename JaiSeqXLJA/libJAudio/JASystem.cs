using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libJAudio;

namespace libJAudio
{
    public class JASystem
    {
        public JIBank[] Banks = new JIBank[0xFF];
        public JWaveSystem[] WaveBanks = new JWaveSystem[0xFF];
        public JAIInitType version;
        
    }

  
}
