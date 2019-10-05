using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JaiSeqX.JAI.Types;
using System.IO;
using Be.IO;
// JABAA Loader
// JAudio Binary Audio Archive Loader
namespace JaiSeqX.JAI.Loaders
{
    class JA_BAALoader
    {
        // BAA - Binary Audio Archive 
        public const int BAA_Header = 0x41415F3C;
        public const int BAAC = 0x62616163; // BINARY AUDIO ARCHIVE CUSTOM
        public const int BFCA = 0x62666361;
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
            if (aafRead.ReadInt32()!=BAA_Header) // Check to see if the header is AA_<
            {
                throw new InvalidDataException("Data is not BAA");  // If it's not, stop because we're obviously int he wrong spot
            }
            while (true)
            {
                var ChunkID = aafRead.ReadInt32(); // 0 chunk-id determines end of header. 
                //Console.WriteLine(ChunkID); // Writing the ID of the chunk for testing.
                if (ChunkID==BAA_Footer) // If the chunk-id is 0, then the array has ended. This means we read >_AA , >_AA being the end of the archive.
                {
                    break; // stop the while.
                }
                order++; // Increment the order for every iteration.
                switch (ChunkID)
                {
                    
                    case 0:
                        throw new Exception(@"Tell me, Dr. Freeman, if you can: you have destroyed so much — what is it exactly that you have created? Can you name even one thing?... I thought not.");  // Not supposed to get here.
                        
                    case BST: // Matches 'BST '
                        {
                            var newSect = new JAIInitSection();
                            newSect.start = aafRead.ReadInt32(); // Read the start offset
                            newSect.size = aafRead.ReadInt32() - newSect.start; // Read the end offset, subtract by the start offset to get the length.
                            newSect.type = JAIInitSectionType.SOUND_TABLE; // Set the type to a sound table
                            stk.Push(newSect); // Push it to the stack
                        }
                        break;
                    case BSTN:
                        {
                            var newSect = new JAIInitSection();
                            newSect.start = aafRead.ReadInt32(); // Read Start offset
                            newSect.size = aafRead.ReadInt32() - newSect.start; // Read end offset, subtract by the start to get the length.
                            newSect.type = JAIInitSectionType.SOUND_TABLE_STRINGS;
                            stk.Push(newSect);
                        }
                        break;

                    case WS:
                        {
                            // WSYS is packed differently.
                            var newSect = new JAIInitSection();
                            newSect.number= aafRead.ReadInt32(); // First number is the global WSYS ID
                            newSect.start = aafRead.ReadInt32(); // Second number is the base offset of the wsys
                            newSect.flags = aafRead.ReadInt32(); // Third are some sort of flags. I think this is used to tell whether or not it's melodic or not.
                            newSect.type = JAIInitSectionType.WSYS; // Set the type to a wsys
                            stk.Push(newSect); // Push into the stack.
                        }
                        break;
                    case BNK:
                        {
                            // Banks also have unique packing, but are simple
                            var newSect = new JAIInitSection();
                            newSect.number = aafRead.ReadInt32();  // The global bank ID.
                            newSect.start = aafRead.ReadInt32(); // The base offset.
                            newSect.type = JAIInitSectionType.IBNK; // And of course, it should be an ibnk
                            stk.Push(newSect);
                        }
                        break;
                    case BSC:
                        {
                            // Sequence collection just has a start and end.
                            var newSect = new JAIInitSection();               
                            newSect.start = aafRead.ReadInt32(); // Read start
                            newSect.size = aafRead.ReadInt32() - newSect.start; // Read end, substract by start to get length.
                            newSect.type = JAIInitSectionType.SEQUENCE_COLLECTION; // And of course BSC type is a sequence collection
                            stk.Push(newSect); // Then push it into the stack.
                        }
                        break;
                    case BMS:
                        {
                            // Embedded BMS are strange.
                            // Their first argument is a number, that starts with 01 then has xx xx xx (at least, from what I can see)
                            // I'm unsure what the purpose of the 01 is, so we'll have to check later to see if it's going to cause issues
                            // (it's probably there for a reason)
                            var newSect = new JAIInitSection();
                            newSect.number = aafRead.ReadInt32() & 0xFFFF; // For some reason this has 01 as the first byte? Bound to cause issues later.
                            // We're just going to take the last 16 bits of it with an & 0xFFFF to trim off that 01.
                            newSect.start = aafRead.ReadInt32(); // Read the start offset
                            newSect.size = aafRead.ReadInt32() - newSect.start; // Read the end offset
                            newSect.type = JAIInitSectionType.MUSIC_SEQUENCE; // Set the tpye to embedded sequence.
                            stk.Push(newSect);
                        }
                        break;
                    case BSFT:
                        {
                            // Unique packing. Only has a start offset.
                            var newSect = new JAIInitSection();
                            newSect.start = aafRead.ReadInt32(); // Read the start offset.                
                            newSect.type = JAIInitSectionType.STREAM_FILE_TABLE;
                            stk.Push(newSect);
                        }
                        break;
                    case BFCA:
                        {
                            // Unique packing. Only has a start offset.
                            var newSect = new JAIInitSection();
                            newSect.start = aafRead.ReadInt32(); // Read the start offset.                
                            newSect.type = JAIInitSectionType.UNKNOWN;
                            stk.Push(newSect);
                        }
                        break;
                    case BAAC:
                        {
                            // This is game-specific data , that we'll have no clue what it does or how it works unless there's some way to detect it --
                            // I'd advice omitting parsing of this section until more is known about a BAAC section.
                            var newSect = new JAIInitSection();
                            newSect.start = aafRead.ReadInt32(); // We do know that it has a start offset
                            newSect.size = aafRead.ReadInt32() - newSect.start; // And an end offset
                            newSect.type = JAIInitSectionType.CUSTOM_DATA; // That's about it.
                            stk.Push(newSect); // oh my ghoood.
                        }
                        break;
                }
                stk.Peek().order = order; // hacky shit lol  
                // I don't keep a reference to the last object, but i know it's at the top of the stack. So I can just peek the top of the stack to grab the last object.
                // Then define the order that it was parsed in.

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
