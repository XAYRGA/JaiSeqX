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
    class JA_IBankLoader_V2
    {
        private const int PERC = 0x50455243; // percussion
        private const int SENS = 0x53454E53; // Sensor effect
        private const int RAND = 0x52414E44; // Random Effect
        private const int OSCT = 0x4F534354; // OSCillator Table
        private const int Osci = 0x4F736369; // Oscillator
        private const int INST = 0x494E5354; // INStrument Table
        private const int Inst = 0x496E7374; // Instrument
        private const int IBNK = 0x49424E4B; // Instrument BaNK
        private const int ENVT = 0x454E5654; // ENVelope Table

        private int iBase = 0;
        private int Boundaries = 0;


        // [!] [!] [!] [!] [!] [!] [!] [!] [!] [!] [!] [!] [!] [!] //
        // [!]  THIS FUNCTION DESTROYS YOUR CURRENT POSITION   [!] //
        // [!] Remember to anchor before calling it or trouble [!] //
        // [!] [!] [!] [!] [!] [!] [!] [!] [!] [!] [!] [!] [!] [!] //
        private int findChunk(BeBinaryReader read, int chunkID, bool immediate = false) 
        {
            if (!immediate) // Indicating we need to search the entire bank (Default)
            {
                read.BaseStream.Position = iBase; // Seek back to IBNK, since i can't follow my own warnings. 
            }
            while (true)
            {
                var pos = (int)read.BaseStream.Position - iBase; // Store the position as an int, relative to ibase. 
                var i = read.ReadInt32(); // Read 4 bytes, since our chunkid is an int32
                if (i==chunkID) // Check to see if the chunk is what we're looking for
                {
                    return pos; // Return position relative to the base. 
                }
                else if (pos > (Boundaries)) // we exceedded boundaries
                {
                    return 0;
                }
            }
        }




    }
}
