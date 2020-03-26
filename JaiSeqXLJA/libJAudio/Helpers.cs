using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;
using System.IO; 

namespace libJAudio
{
    public static class Helpers
    {
        public static uint ReadUInt24BE(BinaryReader reader)
        {
                return
                    (((uint)reader.ReadByte()) << 16) | 
                    (((uint)reader.ReadByte()) << 8) | 
                    ((uint)reader.ReadByte()); 
        }

        public static int ReadVLQ(BinaryReader reader)
        {
            int vlq = 0;
            int temp = 0;
            do
            {
                temp = reader.ReadByte();
                vlq = (vlq << 7) | (temp & 0x7F);
            } while ((temp & 0x80) > 0);
            return vlq;
        }


        public static int[] readInt32Array(BeBinaryReader binStream, int count)
        {
            var b = new int[count];
            for (int i = 0; i < count; i++)
            {
                b[i] = binStream.ReadInt32();
            }

            return b;
        }

    }
}
