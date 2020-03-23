using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;
using System.IO;

namespace libJAudio.Loaders
{


    public static class JAIInitTypeDetector
    {
        /* This class originally had something more clever going on, Zelda Four Swords threw a giant fucking wrench in my code. */
        /* I'd not recommend using it, as it might be removed in the future if the codebase for it doesn't grow. */
        public static JAIInitType checkVersion(ref byte[] data)
        {
            var JStream = new MemoryStream(data);
            var JReader = new BeBinaryReader(JStream);
            var hdr = JReader.ReadUInt32();
            if (hdr == 1094803260) // AA_< LITERAL , opening of BAA archive or BAA Format
            {
                JReader.Close();
                JStream.Close();
                return JAIInitType.BAA; // return BAA type
            }
            else
            { /* PIKMIN BX ARCHIVE */
                /* CHECKING FOR BX 
                * This is not 100% accurate, but the likelyhood of something like this actually getting confused with AAF is slim to none.
                * Considering there's only one game that uses BX. 
                */
                 
                JStream.Position = 0; // reset pos;
                var BXWSOffs = JReader.ReadInt32(); // should point to location in file.
                if (BXWSOffs < JStream.Length) // check if is within BX 
                {
                    JStream.Position = BXWSOffs;
                    var WSO = JReader.ReadInt32();
                    if (WSO < JStream.Length) // fall out, not valid
                    {
                        var WSYS = JReader.ReadInt32();
                        if (WSYS== 0x57535953) // 0x57535953 is literal WSYS
                        {
                            JReader.Close(); // flush / close streams
                            JStream.Close(); // flush and close streams
                            return JAIInitType.BX;
                        }
                    }
                }
            }
             // * The init type is otherwise AAF.
            {
                JReader.Close();
                JStream.Close();
                return JAIInitType.AAF; // JAIInitSection v1 doesn't have an identifying header.
            }
        }
    }
}
