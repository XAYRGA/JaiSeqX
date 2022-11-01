using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Be.IO;


namespace jaudio
{
   public static class util
    {
        /*
        public static string GetFileExtension(JAIInitSectionType type)
        {
            switch (type)
            {
                default:
                case JAIInitSectionType.UNKNOWN:
                case JAIInitSectionType.CUSTOM_DATA:
                    return ".dat";
                case JAIInitSectionType.IBNK:
                    return ".bnk";
                case JAIInitSectionType.MUSIC_SEQUENCE:
                    return ".bms";
                case JAIInitSectionType.SEQUENCE_COLLECTION:
                    return ".arc";
                case JAIInitSectionType.SOUND_TABLE:
                    return ".bst";
                case JAIInitSectionType.SOUND_TABLE_STRINGS:
                    return ".bstn";
                case JAIInitSectionType.STREAM_FILE_TABLE:
                    return ".sft";
                case JAIInitSectionType.STREAM_MAP:
                    return ".stm";
                case JAIInitSectionType.WSYS:
                    return ".wsy";
                case JAIInitSectionType.BDI_DATA:
                    return ".bdi";
            }
        }
        */

        public static int[] readInt32Array(BeBinaryReader binStream, int count)
        {
            var b = new int[count];
            for (int i = 0; i < count; i++)
                b[i] = binStream.ReadInt32();
            return b;
        }


        public static void padTo(BeBinaryWriter bw, int padding)
        {
            while ( (bw.BaseStream.Position%padding) != 0)
                bw.Write((byte)0x00);
        }
    }
}
