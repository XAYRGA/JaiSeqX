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
        SOUND_TABLE = 1,
        IBNK = 2, 
        WSYS = 3,
        SEQUENCE_COLLECTION = 4,
        STREAM_MAP = 5,
        MUSIC_SEQUENCE = 6,
        SOUND_TABLE_STRINGS = 7,
        STREAM_FILE_TABLE = 8,

        CUSTOM_DATA = 254,
        UNKNOWN = 255
    }

    public class JAIInitSection
    {
        public int start;
        public int size;
        public int flags;
        public byte order;
        public int number;
        public JAIInitSectionType type;         
    }

}
