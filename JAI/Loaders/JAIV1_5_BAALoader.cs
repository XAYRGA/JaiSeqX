using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JaiSeqX.JAI.Types;
using System.IO;
using Be.IO;

namespace JaiSeqX.JAI.Loaders
{
    class JAIV1_5_BAALoader
    {
        // BAA - Binary Audio Archive 
        // Loader for V1.5 
        // (This Basically only exists for Double Dash) 
        public const int BAA_Header = 0x41415F3C;
        public const int BAAC = 0x62616163; // BINARY AUDIO ARCHIVE CUSTOM
        public const int BMS = 0x626D7320; // BINARY MUSIC SEQUENCE
        public const int BNK = 0x626E6B20; // INSTRUMENT BANK
        public const int BSC = 0x62736320; // BINARY SEQUENCE COLLECTION
        public const int BSFT = 0x62736674; // BINARY STREAM FILE TABLE
        public const int BST = 0x62737420; // BINARY SOUND TABLE
        public const int BSTN = 0x6273746E; // BINARY SOUND TABLE NAME
        public const int WS = 0x77732020; // WAVE SYSTEM
        public const int BAA_Footer = 0x3E5F4141;
        private JAIInitSection load2PtSection(BeBinaryReader aafRead)
        {
            var NewSect = new JAIInitSection();
            var offset = aafRead.ReadInt32();
            var size = aafRead.ReadInt32();
            NewSect.start = offset;
            NewSect.size = size;
            NewSect.flags = 0;
            return NewSect;
        }

        public JAIInitSection[] load(ref byte[] data)
        {
            Stack<JAIInitSection> stk = new Stack<JAIInitSection>(255);
            var aafRead = new BeBinaryReader(new MemoryStream(data));
            byte order = 0;
            if (aafRead.ReadInt32()!=BAA_Header)
            {
                throw new InvalidDataException("Data is not BAA");
            }
            while (true)
            {
                var ChunkID = aafRead.ReadInt32(); // 0 chunk-id determines end of header. 
                Console.WriteLine(ChunkID);
                if (ChunkID==BAA_Footer) // If the chunk-id is 0, then the  array  has  ended. 
                {
                    break; // break loop
                }
                order++;
                switch (ChunkID)
                {
                    case BST:
                        {
                            var newSect = new JAIInitSection();
                            newSect.start = aafRead.ReadInt32();
                            newSect.size = aafRead.ReadInt32() - newSect.start;
                            newSect.type = JAIInitSectionType.SOUND_TABLE;
                            stk.Push(newSect);
                        }
                        break;
                    case BSTN:
                        {
                            var newSect = new JAIInitSection();
                            newSect.start = aafRead.ReadInt32();
                            newSect.size = aafRead.ReadInt32() - newSect.start;
                            newSect.type = JAIInitSectionType.SOUND_TABLE_STRINGS;
                            stk.Push(newSect);
                        }
                        break;

                    case WS:
                        {
                            var newSect = new JAIInitSection();
                            newSect.number= aafRead.ReadInt32(); 
                            newSect.start = aafRead.ReadInt32(); 
                            newSect.flags = aafRead.ReadInt32(); // Might be hacky, but i need to fit it in this container.
                            newSect.type = JAIInitSectionType.WSYS;
                            stk.Push(newSect);
                        }
                        break;
                    case BNK:
                        {
                            var newSect = new JAIInitSection();
                            newSect.number = aafRead.ReadInt32();
                            newSect.start = aafRead.ReadInt32();
                            newSect.type = JAIInitSectionType.IBNK;
                            stk.Push(newSect);
                        }
                        break;
                    case BSC:
                        {
                            var newSect = new JAIInitSection();
               
                            newSect.start = aafRead.ReadInt32();
                            newSect.size = aafRead.ReadInt32() - newSect.start;
                            newSect.type = JAIInitSectionType.SEQUENCE_COLLECTION;
                            stk.Push(newSect);
                        }
                        break;
                    case BMS:
                        {
                            var newSect = new JAIInitSection();
                            newSect.number = aafRead.ReadInt32() & 0xFFFF; // For some reason this has 01 as the first byte? Bound to cause issues later.
                            newSect.start = aafRead.ReadInt32();
                            newSect.size = aafRead.ReadInt32() - newSect.start;
                            newSect.type = JAIInitSectionType.MUSIC_SEQUENCE;
                            stk.Push(newSect);
                        }
                        break;
                    case BSFT:
                        {
                            var newSect = new JAIInitSection();
                            newSect.start = aafRead.ReadInt32();                       
                            newSect.type = JAIInitSectionType.STREAM_FILE_TABLE;
                            stk.Push(newSect);
                        }
                        break;
                    case BAAC:
                        {
                            var newSect = new JAIInitSection();
                            newSect.start = aafRead.ReadInt32();
                            newSect.size = aafRead.ReadInt32() - newSect.start;
                            newSect.type = JAIInitSectionType.CUSTOM_DATA;
                            stk.Push(newSect);
                        }
                        break;
                }
                stk.Peek().order = order;            
            }
            var stackLen = stk.Count; // Grab how many entries are inside of the stack.
            JAIInitSection[] sectionData = new JAIInitSection[stackLen]; //  Mmake an array of tht size
            for (int i = stackLen - 1; i > -1; i--) // unroll the stack into an array in reverse (since we stacked it in reverse.)
            {
                var obj = stk.Pop(); // Pull the next thing off of the top of the stack.
                sectionData[i] = obj; // Throw it into the array.
            }
            return sectionData; // Finally, return the array.
        }
     

    
        
     
    }
}
