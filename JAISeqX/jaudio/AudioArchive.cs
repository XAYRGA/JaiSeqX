using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;

namespace jaudio
{

    public enum AAFChunkType
    {
        WSYS,

        IBNK,


        SOUNDTABLE1,
        SOUNDTABLE2,
        SOUNDTABLE2_NAMES,

        BUILDINFO,

        SOUNDCOLLECTION,

        SEQBARC,

        STREAMTABLE1,
        STREAMTABLE2,

        STREAMTABLE_PIKMIN,
        STREAMTABLE_DOUBLEDASH,

        FCDATA,

        UNKNOWN = 0xFF
    }

    internal partial class AudioArchive
    {
        public List<AudioArchiveSectionInfo> Sections = new();

        public static AudioArchive CreateFromStream(BeBinaryReader rd)
        {
            var a = new AudioArchive();
            a.load(rd);
            return a;
        }

        public virtual void load(BeBinaryReader rd)
        {
            var go = true;
            while (go)
            {
                var ChunkType = rd.ReadInt32();
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
                            offset = rd.ReadInt32();
                            size = rd.ReadInt32();
                            flags = rd.ReadInt32();
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
                            }

                            Sections.Add(new AudioArchiveSectionInfo(type, offset, size, flags));
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
                                offset = rd.ReadInt32();
                                if (offset == 0)
                                    break;
                                size = rd.ReadInt32();
                                flags = rd.ReadInt32();
                                
                                Sections.Add(new AudioArchiveSectionInfo(type,offset,size,flags));
                            }
                            break;
                       }
                    case 0:
                        go = false;
                        break;
                }
            }
     
            for (int i=0; i < Sections.Count; i++)
            {
                var sect = Sections[i];
                rd.BaseStream.Position = sect.offset;
                sect.stream = new MemoryStream(rd.ReadBytes(sect.size));
                sect.reader = new BeBinaryReader(sect.stream);
                sect.writer = new BeBinaryWriter(sect.stream);
            }
        }
    }

    internal class AudioArchiveSectionInfo
    {
        public AAFChunkType type;
        public int offset;
        public int size;
        public int flags;
        public Stream stream;
        public BeBinaryReader reader;
        public BeBinaryWriter writer;
        public object obj;

        public AudioArchiveSectionInfo(AAFChunkType type, int offset, int size, int flags)
        {
            this.type = type;
            this.offset = offset;
            this.size = size;
            this.flags = flags;
        }

    }
}
