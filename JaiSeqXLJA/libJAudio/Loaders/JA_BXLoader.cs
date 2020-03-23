using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Be.IO;

namespace libJAudio.Loaders
{
    public class JA_BXLoader
    {
        public JAIInitSection[] load(ref byte[] data)
        {
            Stack<JAIInitSection> stk = new Stack<JAIInitSection>(255);
            var aafRead = new BeBinaryReader(new MemoryStream(data));
            byte order = 0;

            // Parse Header 
            var wsPointerOffset = aafRead.ReadInt32();
            var wsPointerCount = aafRead.ReadInt32();

            var ibnkPointerOffset = aafRead.ReadInt32();
            var ibnkPointerCount = aafRead.ReadInt32();

            // load WSYS sections
            order = 0; // Reset Order
            aafRead.BaseStream.Position = wsPointerOffset;
            for (int i = 0; i < wsPointerCount; i++)
            {
                var wsOffset = aafRead.ReadInt32();
                var wsLength = aafRead.ReadInt32();
                if (wsLength > 0)
                {
                    var nWSJIS = new JAIInitSection()
                    {
                        order = order,
                        start = wsOffset,
                        size = wsLength,
                        number = 0,
                        type = JAIInitSectionType.WSYS,
                        flags = 0,
                    };
                    stk.Push(nWSJIS);
                    order++;
                }
            }
            // Load IBNK sections
            order = 0;  // reset load order
            aafRead.BaseStream.Position = ibnkPointerOffset;
            for (int i = 0; i < ibnkPointerCount; i++)
            {
                var ibOffset = aafRead.ReadInt32();
                var ibLength = aafRead.ReadInt32();
                if (ibLength > 0)
                {
                    var nWSJIS = new JAIInitSection()
                    {
                        order = order,
                        start = ibOffset,
                        size = ibLength,
                        number = 0,
                        type = JAIInitSectionType.IBNK,
                        flags = 1,
                    };
                    stk.Push(nWSJIS);
                    order++;
                }
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

