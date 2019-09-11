using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JaiSeqX.JAI.Types
{
    public enum JAIInitSectionType
    {
        END = 0,
        TUNING_TABLE = 1,
        IBNK = 2, 
        WSYS = 3,
        SEQMAP = 4,
        STREAM_MAP = 5,


        UNKNOWN = 255

    }

    public class JAIInitSection
    {
        public int start;
        public int size;
        public int flags;
        public byte order;
        public JAIInitSectionType type;         
    }
}
