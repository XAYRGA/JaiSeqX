using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JaiSeqX.JAI.Types;

namespace JaiSeqX.JAI.Loaders
{
    public static class JASystemLoader
    {
        public static JASystem loadJASystem(ref byte[] data)
        {
            var newJA = new JASystem();
            var version = JASystemVersionDetector.checkVersion(ref data);
            newJA.version = version;
            switch (version)
            {
                case JAIVersion.ZERO:
                    loadJV0(ref newJA, ref data);
                    break;
            }
            return newJA;
        }

        public static void loadJV0(ref JASystem JAS, ref byte[] data)
        {
            var ldr = new JA_BAALoader();
            var sections = ldr.load(ref data);
            var stm = new System.IO.MemoryStream(data);
            var read = new Be.IO.BeBinaryReader(stm);

            for (int i=0; i < sections.Length; i++)
            {
                var current_section = sections[i];
                switch(current_section.type)
                {
                    case JAIInitSectionType.IBNK:
                        {
                            stm.Position = current_section.start;
                            var vx = new JA_IBankLoader_V1();
                            var ibnk = vx.loadIBNK(read, current_section.start);
                            JAS.Banks[ibnk.id] = ibnk;
                            break;
                        }
                    case JAIInitSectionType.WSYS:
                        {
                            stm.Position = current_section.start;
                            var vx = new JA_WSYSLoader_V1();
                            var ws = vx.loadWSYS(read, current_section.start);
                            JAS.WaveBanks[ws.id] = ws;
                            break;
                        }
                    case JAIInitSectionType.SEQUENCE_COLLECTION:
                        {

                            break;
                        }
                }
            }
           
        }

    }
}
