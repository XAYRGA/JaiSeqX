using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;
using System.IO;

namespace JaiSeqX.JAI.Loaders
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
            if (hdr==1094803260) // AA_<
            {
                JReader.Close();
                JStream.Close();
                return JAIInitType.BAA;
            } else
            {
                JReader.Close();
                JStream.Close();
                return JAIInitType.AAF; // JAIInitSection v1 doesn't have an identifying header.
            }
        }

    }
}
