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
    class JAIV1_5_BAALoader
    {
        public const int BAA_Header = 0x41415F3C;
        public const int BAAC = 0x62616163;
        public const int BMS = 0x626D7320;
        public const int BNK = 0x626E6B20;
        public const int BSFT = 0x62736674;
        public const int BST = 0x62737420;
        public const int BSTN = 0x6273746E;
        public const int WS = 0x77732020;

        private JAIInitSection loadRegularSection(BeBinaryReader aafRead)
        {
            var NewSect = new JAIInitSection();
            var offset = aafRead.ReadInt32();
            var size = aafRead.ReadInt32();
            var type = aafRead.ReadInt32();
            NewSect.start = offset;
            NewSect.size = size;
            NewSect.flags = type;
            return NewSect;
        }
     
    
        
     
    }
}
