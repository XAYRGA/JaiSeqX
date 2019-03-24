using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JaiSeqX.JAI.Seq;
using JaiSeqX.JAI.Types;
using JaiSeqX.JAI.Types.WSYS;
using System.IO;
using Be.IO;

namespace JaiSeqX.JAI
{
    

    public class AAFFile : AABase
    {
     
        private string convertChunkName(uint id)
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
                default:
                    return "Unknown Chunk (Alignment kept)";
            }
        }

        public void LoadAAFile(string filename, JAIVersion version)
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
                Console.WriteLine("[AAF] Found chunk: {0}", name);
                
                switch (ChunkID)
                {
                    case 0:                        
                        done = true; // either we're misalligned or done, so just stopo here. 
                        break;
                    case 1: // Don't know 
                    case 5:
                    case 4:
                    case 6:
                    case 7:
                        aafRead.ReadUInt32();
                        aafRead.ReadUInt32();
                        aafRead.ReadUInt32();
                        break;
                    case 2: // INST
                    case 3: // WSYS 
                        {
                            while (true)
                            {

                                var offset = aafRead.ReadUInt32();
                                if (offset == 0)
                                {
                                    break;  // 0 means we reached the end.
                                }
                                var size = aafRead.ReadUInt32();
                                var type = aafRead.ReadUInt32();



                                anchor = aafRead.BaseStream.Position; // Store our return position. 

                                aafRead.BaseStream.Position = offset; // Seek to the offset pos. 
                                if (ChunkID==3)
                                {
                                   
                                        var b = new WaveSystem(); // Load the wavesystem
                                        b.LoadWSYS(aafRead);
                                        WSYS[b.id] = b; // store it

                                    Console.WriteLine("\t WSYS at 0x{0:X}", offset);
                                } else if (ChunkID==2)
                                {
                                    
                                    var x = new InstrumentBank();
                                    x.LoadInstrumentBank(aafRead,version);
                                    Console.WriteLine("\t IBNK at 0x{0:X}", offset);
                                    IBNK[x.id] = x; // Store it 
                                }
                                aafRead.BaseStream.Position = anchor; // Return back to our original pos after loading. 

                            }
                            break;
                        }
           



                }
            }


        }
    }
}
