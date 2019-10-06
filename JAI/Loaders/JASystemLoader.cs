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
            var version = JAIInitTypeDetector.checkVersion(ref data);
            newJA.version = version;
            switch (version)
            {
                case JAIInitType.AAF:
                    loadJV0(ref newJA, ref data);
                    break;
                case JAIInitType.BAA:
                    loadJV2(ref newJA, ref data);
                    break;
            }
            return newJA;
        }


        /* AAF is safe, we know it's always going to be a V1 instrument format. */
        public static void loadJV0(ref JASystem JAS, ref byte[] data)
        {
            var ldr = new JA_AAFLoader(); // Create the loader for the AAF.
            var sections = ldr.load(ref data); // Load the AAF
            var stm = new System.IO.MemoryStream(data); // Create a stream for it
            var read = new Be.IO.BeBinaryReader(stm); // Create a reader around the stream
             
            for (int i=0; i < sections.Length; i++) // Loop through the AAF
            {
                var current_section = sections[i]; // Select current section
                switch(current_section.type) // Get the type
                {
                    case JAIInitSectionType.IBNK: // Instrument bank
                        {
                            stm.Position = current_section.start; // Seek to start
                            var vx = new JA_IBankLoader_V1(); // Make a loader
                            var ibnk = vx.loadIBNK(read, current_section.start); // Load it
                            JAS.Banks[ibnk.id] = ibnk; //Push into bank array.
                            break;
                        }
                    case JAIInitSectionType.WSYS: // Wave System
                        {
                            stm.Position = current_section.start; // Seek to start
                            var vx = new JA_WSYSLoader_V1(); // Create loader
                            var ws = vx.loadWSYS(read, current_section.start); // load
                            JAS.WaveBanks[ws.id] = ws; // Push to wavebanks
                            break;
                        }
                    case JAIInitSectionType.SEQUENCE_COLLECTION:
                        {
                            break;
                        }
                }
            }
           
        }

        public static void loadJV2(ref JASystem JAS, ref byte[] data)
        {
            var ldr = new JA_BAALoader(); // Create the loader for the AAF.
            var sections = ldr.load(ref data); // Load the AAF
            var stm = new System.IO.MemoryStream(data); // Create a stream for it
            var read = new Be.IO.BeBinaryReader(stm); // Create a reader around the stream

            for (int i = 0; i < sections.Length; i++) // Loop through the AAF
            {
                var current_section = sections[i]; // Select current section
                switch (current_section.type) // Get the type
                {
                    case JAIInitSectionType.IBNK: // Instrument bank -- alright seriously fuck these they can be either v1 or v2 in baa, sometimes mixed.
                        {
                            stm.Position = current_section.start; // Seek to start
                            stm.Seek(0x20, System.IO.SeekOrigin.Current); // have to detect the type.
                            var secondChunkID = read.ReadInt32();

                            JIBank ibnk;
                            if (secondChunkID == 0x42414E4B) // V1 type banks always have literal `BANK` after them. 
                            {
                                 var v1L = new JA_IBankLoader_V1(); // Make a loader
                                 ibnk = v1L.loadIBNK(read, current_section.start); // Load it
                                JAS.Banks[ibnk.id] = ibnk; //Push into bank array. 
                                break;
                            }

                            stm.Position = current_section.start; // Reset stream position to section base.
                            // Though V2 has 'ENVT' just after it, don't check for it. It's either v1 or v2.                              
                            var v2L = new JA_IBankLoader_V2(); // Make a loader
                             ibnk = v2L.loadIBNK(read, current_section.start); // Load it
                             JAS.Banks[ibnk.id] = ibnk; //Push into bank array. 
                            break;
                        }
                    case JAIInitSectionType.WSYS: // Wave System -- the same as jaiv1?
                        {
                            stm.Position = current_section.start; // Seek to start
                            var vx = new JA_WSYSLoader_V1(); // Create loader
                            var ws = vx.loadWSYS(read, current_section.start); // load
                            JAS.WaveBanks[ws.id] = ws; // Push to wavebanks
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
