using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JaiSeqX.JAI.Seq;
using JaiSeqX.JAI.Types;
using JaiSeqX.JAI.Types.WSYS;
using Be.IO;
using System.IO; 

namespace JaiSeqX.JAI
{
   
    public class BAAFile : AABase
    {

            private string convertChunkName(uint id)
            {
                switch (id)
                {
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

            public void LoadBAAFile(string filename, JAIVersion version)
            {
                WSYS = new WaveSystem[0xFF]; // None over 256 please :).
                IBNK = new InstrumentBank[0xFF]; // These either. 
                var aafdata = File.ReadAllBytes(filename);  // We're just going to load a whole copy into memory because we're lame -- having this buffer in memory makes it easy to pass as a ref to stream readers later. 
                var aafRead = new BeBinaryReader(new MemoryStream(aafdata));

                bool done = false;

                while (!done)
                {
                    var ChunkID = aafRead.ReadUInt32();
                    long anchor;
                    var name = convertChunkName(ChunkID);
                    Console.WriteLine("[BAA] Found chunk: {0}", name);

                    switch (ChunkID)
                    {
                        case 1046430017: // >_AA
                            done = true; // either we're misalligned or done, so just stopo here. 
                            break;
                        case 1094803260: // AA_<
                            break;
                        case 1651733536: // BST
                            {
                                var offset_sta = aafRead.ReadUInt32();
                                var offset_end = aafRead.ReadUInt32();
                                break;
                            }
                        case 1651733614: // BSTN
                            {
                                var offset_sta = aafRead.ReadUInt32();
                                var offset_end = aafRead.ReadUInt32();
                                break;
                            }
                        case 1651729184: // BSC
                            {
                                var offset_sta = aafRead.ReadUInt32();
                                var offset_end = aafRead.ReadUInt32();
                                break;
                            }
                 
                        case 1651403552: // BNK 
                            {
                                var id = aafRead.ReadUInt32();
                                var offset = aafRead.ReadUInt32();
                                anchor = aafRead.BaseStream.Position; // Store our return position. 
                                aafRead.BaseStream.Position = offset; // Seek to the offset pos. 
                                var b = new InstrumentBank();
                                b.LoadInstrumentBank(aafRead, version); // Load it up
                                IBNK[id] = b;
                                aafRead.BaseStream.Position = anchor; // Return back to our original pos after loading. 

                                break;
                            }
                        case 2004033568: // WSYS 
                            {
                                var id = aafRead.ReadUInt32();
                                var offset = aafRead.ReadUInt32();
                                var flags = aafRead.ReadUInt32();

                                anchor = aafRead.BaseStream.Position; // Store our return position. 
                                aafRead.BaseStream.Position = offset; // Seek to the offset pos. 
                                var b = new WaveSystem();
                                b.LoadWSYS(aafRead);
                                WSYS[id] = b; 
                                aafRead.BaseStream.Position = anchor; // Return back to our original pos after loading. 
                                break;
                            }
                               
                    }
                }
             }
         }
     }
 

