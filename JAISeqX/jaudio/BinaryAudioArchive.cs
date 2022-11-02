using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;

namespace jaudio
{
    internal partial class BinaryAudioArchive : AudioArchive
    {
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
        public const int Footer = 0x3E5F4141;



        public static new AudioArchive CreateFromStream(BeBinaryReader rd)
        {
            var a = new BinaryAudioArchive();
            a.load(rd);
            return a;
        }

        private int baa_GetSectionSize(AudioArchiveSectionInfo sect, BeBinaryReader br)
        {
            switch (sect.type)
            {
                // The following is true for both V1-Type banks
                case AAFChunkType.IBNK:
                    br.ReadUInt32(); // Skip IBNK header.
                    return br.ReadInt32() + 8; // next operator is size
                case AAFChunkType.WSYS:
                    br.ReadUInt32(); // Skip WSYS header.
                    return br.ReadInt32() + 8; // next operator is size
                case AAFChunkType.UNKNOWN:
                    br.ReadInt32();
                    return br.ReadInt32();
                default:
                    return sect.size;                 
            }
        }


        public override void load(BeBinaryReader rd)
        {
            var header = rd.ReadUInt32();
            if (header != BAA_Header)
                throw new Exception("Input data is not BAA data!");

            var go = true;
            while (go)
            {
                var type = AAFChunkType.UNKNOWN;
                var ChunkType = rd.ReadInt32();
                var offset = 0;
                var size = 0;
                var id = 0;
                var flags = 0;
                switch (ChunkType)
                {
                    case BST:
                        offset = rd.ReadInt32();
                        size = rd.ReadInt32();
                        type = AAFChunkType.SOUNDTABLE2;
                        break;
                    case BSTN:
                        offset = rd.ReadInt32();
                        size = rd.ReadInt32();
                        type = AAFChunkType.SOUNDTABLE2_NAMES;
                        break;
                    case WS:
                        id = rd.ReadInt32();
                        offset = rd.ReadInt32();
                        size = rd.ReadInt32();
                        type = AAFChunkType.WSYS;
                        break;
                    case BNK:
                        id = rd.ReadInt32();
                        offset = rd.ReadInt32();
                        type = AAFChunkType.IBNK;
                        break;
                    case BSC:
                        offset = rd.ReadInt32();
                        size = rd.ReadInt32();
                        type = AAFChunkType.SOUNDCOLLECTION;
                        break;
                    case BMS:
                        id = rd.ReadInt32();
                        offset = rd.ReadInt32();
                        size = rd.ReadInt32();
                        type = AAFChunkType.SEQUENCE;
                        break;
                    case BFCA:
                        offset = rd.ReadInt32();
                        type = AAFChunkType.UNKNOWN;
                        break;
                    case Footer:
                        go = false;
                        break;
                    default:
                        throw new Exception($"Unknown section type 0x{ChunkType:X} @ 0x{rd.BaseStream.Position:X}");      
                }

                if (size > 0)
                    size = size - offset; // Normalize sizes

                if (go)
                    Sections.Add(new AudioArchiveSectionInfo(type, offset, size, flags, id));
            }




            for (int i = 0; i < Sections.Count; i++)
            {
                var sect = Sections[i];
                rd.BaseStream.Position = sect.offset;
                sect.size = baa_GetSectionSize(sect,rd);
                rd.BaseStream.Position = sect.offset;
                sect.stream = new MemoryStream(rd.ReadBytes(sect.size));
                sect.reader = new BeBinaryReader(sect.stream);
                sect.writer = new BeBinaryWriter(sect.stream);
            }
        }
    }
}
