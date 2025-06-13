using System.Linq.Expressions;
using xayrga.byteglider;

namespace jaudio
{
    internal class AudioArchiveFile : AudioArchive
    {

        public AudioArchiveFile() : base()
        {
            Type = AudioArchiveType.AAF;            
        }

        public static AudioArchiveFile CreateFromStream(bgReader rd)
        {
            var a = new AudioArchiveFile();
            a.load(rd);
            return a;
        }

        public override void load(bgReader rd)
        {
            var go = true;
            while (go)
            {
                var ChunkType = rd.ReadInt32BE();
                var offset = 0;
                var size = 0;
                var flags = 0;
                switch (ChunkType)
                {
                    case 1:
                    case 5:
                    case 4:
                    case 6:
                    case 7:
                    case 8:
                        {
                            var type = AAFChunkType.UNKNOWN;
                            offset = rd.ReadInt32BE();
                            size = rd.ReadInt32BE();
                            flags = rd.ReadInt32BE();
                            switch (ChunkType)
                            {
                                case 4:
                                    type = AAFChunkType.SEQBARC;
                                    break;
                                case 1:
                                    type = AAFChunkType.SOUNDTABLE1;
                                    break;
                                case 10:
                                    type = AAFChunkType.BUILDINFO;
                                    break;
                                case 9:
                                    type = AAFChunkType.FCDATA;
                                    break;
                                case 5:
                                    type = AAFChunkType.STREAMTABLE1;
                                    break;
                                case 6:
                                    type = AAFChunkType.BANK_MAP;
                                    break;
                                case 7:
                                    type = AAFChunkType.REVERB_PARAM;
                                    break;
                                       
                            }
                            Sections.Add(new AudioArchiveSectionInfo(type, offset, size, flags));
#if DEBUG
                            Console.WriteLine($"aaf: {type}({ChunkType})@0x{offset:X} Size={size:X}");
#endif
                            break;
                        }
                    case 2:
                    case 3:
                        {
                            while (true)
                            {
                                var type = AAFChunkType.IBNK;
                                if (ChunkType == 3)
                                    type = AAFChunkType.WSYS;
                                offset = rd.ReadInt32BE();
                                if (offset == 0)
                                    break;
                                size = rd.ReadInt32BE();
                                flags = rd.ReadInt32BE();
#if DEBUG
                                Console.WriteLine($"aaf: bank {type}({ChunkType})@0x{offset:X} Size={size:X}");
#endif
                                Sections.Add(new AudioArchiveSectionInfo(type, offset, size, flags));

                            }
                            break;
                        }
                    case 0:
                        go = false;
                        break;
                    default:
                        throw new Exception($"aaf: Unknown chunk type {ChunkType:X}@{rd.BaseStream.Position}");
                }
            }

            for (int i = 0; i < Sections.Count; i++)
            {
                var sect = Sections[i];
                rd.BaseStream.Position = sect.offset;
                sect.stream = new MemoryStream(rd.ReadBytes(sect.size));
                sect.reader = new bgReader(sect.stream);
                sect.writer = new bgWriter(sect.stream);
            }
        }
    }
}
