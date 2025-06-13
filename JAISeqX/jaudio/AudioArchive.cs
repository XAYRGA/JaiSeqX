using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xayrga.byteglider;

namespace jaudio
{
    public enum AAFChunkType
    {
        WSYS,

        IBNK,
        IBNK2,


        SOUNDTABLE1,
        SOUNDTABLE2,
        SOUNDTABLE2_NAMES,

        BUILDINFO,
        REVERB_PARAM,
        BANK_MAP,


        SOUNDCOLLECTION,

        SEQUENCE,
        SEQBARC,

        STREAMTABLE1,
        STREAMTABLE2,

        STREAMTABLE_PIKMIN,
        STREAMTABLE_DOUBLEDASH,

        FCDATA,

        UNKNOWN = 0xFF
    }

    public enum AudioArchiveType
    {
        AAF,
        BAA,
        BX
    }

    public abstract class AudioArchive
    {
        public string Game = "";
        public AudioArchiveType Type;
        public List<AudioArchiveSectionInfo> Sections = new();

        public abstract void load(bgReader read);
    }

    public class AudioArchiveSectionInfo
    {
        public AAFChunkType type;
        public int id;
        public int offset;
        public int size;
        public int flags;
        public Stream stream;
        public bgReader reader;
        public bgWriter writer;
        public object obj;

        public AudioArchiveSectionInfo(AAFChunkType type, int offset, int size, int flags, int id = 0)
        {
            this.id = id;
            this.type = type;
            this.offset = offset;
            this.size = size;
            this.flags = flags;
        }

    }
}
