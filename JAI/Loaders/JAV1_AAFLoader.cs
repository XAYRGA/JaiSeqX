using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JaiSeqX.JAI.Types;
using Be.IO;
using System.IO;

namespace JaiSeqX.JAI.Loaders
{
    class JAV1_AAFLoader
    {


        /* AAF 
         
            while (cunkID!=0) {
                int32 chunkID
                int32 offset
                int32 size
                int32 type 

            }


            chunkid = 3 or chunkid = 2: 
                while (offset!=0) {
                    int32 offset
                    int32 size
                    int32 flags
                }            
          
        */

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

        public JAIInitSection[] load(ref byte[] data)
        {
            Stack<JAIInitSection> stk = new Stack<JAIInitSection>(255);
            var aafRead = new BeBinaryReader(new MemoryStream(data));
            byte order = 0;
            while (true)
            {
                var ChunkID = aafRead.ReadInt32(); // 0 chunk-id determines end of header. 
                Console.WriteLine(ChunkID);
                if (ChunkID==0) // If the chunk-id is 0, then the  array  has  ended. 
                {
                    break; // break loop
                }
                
                switch (ChunkID)  // Map chunk id's to types.
                {
                    case 0:
                        throw new Exception(@"Tell me, Dr. Freeman, if you can: you have destroyed so much — what is it exactly that you have created? Can you name even one thing?... I thought not.");  // Not supposed to get here.

                    default:  // Is Just a regular section.
                        {
                            var NewSect = loadRegularSection(aafRead); // Load the regular  section
                            NewSect.type = JAIInitSectionType.UNKNOWN; // Don't know what type it is, wasn't in the case
                            NewSect.order = order; // The order it was loaded in  (for later reassembly)
                            order++; // Increment Order
                            stk.Push(NewSect); // Push to return stack
                        }
                        break;
                    case 1:
                        {
                            var NewSect = loadRegularSection(aafRead); // Load regular section
                            NewSect.type = JAIInitSectionType.TUNING_TABLE; // Type 1 is the finetune table.
                            NewSect.order = order; // The order it was loaded in  (for later reassembly)
                            order++; // Increment Order 
                            stk.Push(NewSect); // Push to return stack
                            break;
                        }
                    case 2: // IBNK
                    case 3: // WSYS are special systemms, they have  an indicator, then a table of values, then a terminator, then they continue.
                        {
                            while (true)
                            {
                                var offset = aafRead.ReadInt32(); // Offset will be the determining factor of whether or not to  stop, if  it's 0, stop, otherwise, that's the offset of your data
                                // Past this, this is just a bunch of typeless "regular" sections squished together.
                                if (offset == 0)
                                {
                                    break; // 0-based  offset  indicates end of seciton.
                                }
                                var size = aafRead.ReadInt32();  // Read size
                                var type = aafRead.ReadInt32(); // Read the type / flags (flags)
                                var NewSect = new JAIInitSection(); // Create new section object
                                if (ChunkID == 2) // This is just for setting the internal type. 2 is IBNK, 3 is WSYS
                                {
                                    NewSect.type = JAIInitSectionType.IBNK; // IBNK
                                }
                                else
                                {
                                    NewSect.type = JAIInitSectionType.WSYS; // WSYS
                                }
                                NewSect.start = offset; // Starts at offset
                                NewSect.size = size; // Is of size 
                                NewSect.flags = type; // Flags
                                NewSect.order = order;  // Order
                                order++; // Increment order after storing.
                              
                                stk.Push(NewSect); // Push to return stack
                            }
                            break;
                        }
                    case 4:
                        {
                            var NewSect = loadRegularSection(aafRead); // Load regular section
                            NewSect.type = JAIInitSectionType.SEQMAP;// Type 4 would be the sequence table.
                            NewSect.order = order; // Store order.
                            order++; // Increment global order
                            stk.Push(NewSect); // Push to return stack
                            break;
                        }
                    case 5:
                        {
                            var NewSect = loadRegularSection(aafRead); // Load regular section
                            NewSect.type = JAIInitSectionType.STREAM_MAP; // Type 5 is stream map
                            NewSect.order = order; // Store Order
                            order++; // Then incremenet
                            stk.Push(NewSect); // Then push  to return stack
                            break;
                        }

                }
            }

            var stackLen = stk.Count; // Grab how many entries are inside of the stack.
            JAIInitSection[] sectionData = new JAIInitSection[stackLen]; //  Mmake an array of tht size
            for (int i=stackLen - 1; i > -1; i--) // unroll the stack into an array in reverse (since we stacked it in reverse.)
            {
                var obj = stk.Pop(); // Pull the next thing off of the top of the stack.
                sectionData[i] = obj; // Throw it into the array.
            }
            return sectionData; // Finally, return the array.
        }


    }
}
