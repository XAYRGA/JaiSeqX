using jaudio.sequence;
using xayrga.byteglider;

namespace jaudio.sequence
{

    public abstract class ISequenceReader
    {
        internal Dictionary<byte,Type> InstructionMapping = new Dictionary<byte, Type>();
        public abstract SequenceCommand readNextCommand(bgReader reader);
    }
}
