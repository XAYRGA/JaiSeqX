using jaudio.sequence;
using xayrga.byteglider;

namespace JAudioStudio.jaudio.sequence
{

    internal abstract class ISequenceReader
    {
        internal Dictionary<byte,Type> InstructionMapping = new Dictionary<byte, Type>();
        public abstract SequenceCommand readNextCommand(bgReader reader);
    }
}
