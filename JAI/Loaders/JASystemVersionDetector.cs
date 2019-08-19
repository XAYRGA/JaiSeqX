using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;
using System.IO;

namespace JaiSeqX.JAI.Loaders
{


    public static class JASystemVersionDetector
    {
        private static string convertChunkName(uint id)
        {
            switch (id)
            {
                case 0:
                    return "End marker";
                case 4:
                    return "Sequence Map Pointer";
                case 2:
                    return "IBNK Pointer Table";
                case 3:
                    return "WaveSystem Pointer Table";
                case 1094803260:
                    return "Start Marker";
                case 1651733536:
                    return "BST Section";
                case 1651733614:
                    return "BSTN Section";
                case 1651729184:
                    return "BSC Section";
                case 2004033568:
                    return "WSYS pointer";
                case 1651403552:
                    return "IBNK Pointer";
                case 1046430017:
                    return "End Marker";
                default:
                    return "Unknown Chunk (Alignment kept)";
            }

           
        }


        public static JAIVersion checkVersion(ref byte[] data)
        {
            var JStream = new MemoryStream(data);
            var JReader = new BeBinaryReader(JStream);
            var hdr = JReader.ReadUInt32();
            if (hdr==1094803260) // AA_<
            {
                // There are two versions of this format, and no differentiating charactersistics in the header -- only in the data. 
                // So we'll have to seek each chunk until we can find the data we're looking for
                var cookie = true;
                while (cookie)
                {
                    var ChunkType = JReader.ReadUInt32();
                    switch (ChunkType)
                    {
                        case 2004033568: // WSYS
                            {
                                JReader.ReadInt32(); // ID 
                                JReader.ReadInt32(); // Offset
                                JReader.ReadInt32(); // Flags
                                break;
                            }
                        case 1651403552: // bnk
                            {
                                var id = JReader.ReadInt32(); // Unused
                                var offset = JReader.ReadInt32(); // Offset of ibank data
                                JReader.BaseStream.Position = offset; // seek to offset;
                                // "WHY DOES THIS WORK"? // 
                                // JAI1-5 had the new BAA format, but still used the same IBNK format, which always 100% of the time has "BANK" 0x20 bytes after it.
                                // This means that if BANK is after it, then its JAI1-5, otherwise, it's not, but it still had the BAA header, which leaves the only remaining possibility JAIV2.
                                JReader.ReadBytes(0x20); // Skip 0x20 bytes.
                                var idata = JReader.ReadInt32(); 
                                if (idata == 0x42414E4B) // Should read BANK.
                                {
                                    JReader.Close();
                                    JStream.Close();
                                    return JAIVersion.ONE;
                                }
                                JReader.Close();
                                JStream.Close();
                                return JAIVersion.TWO;
                            }
                        default:
                            {
                                JReader.ReadInt32(); // Offset
                                JReader.ReadInt32(); // End
                                break;
                            }
                    }
                }
                // I have to have this, 
                // It seems C# doesn't understand that that loop will never break until it either returns or errors.
                return JAIVersion.ONE;
            } else
            {
                JReader.Close();
                JStream.Close();
                return JAIVersion.ZERO; // JAIInit v1 doesn't have an identifying header.
            }
        }

    }
}
