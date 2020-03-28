using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libJAudio
{
    public class JEnvelope
    {
        public static JEnvelopeVector zeroVector = new JEnvelopeVector { mode = JEnvelopeVectorMode.Linear, time = 0, value = 0 };
        public JEnvelopeVector[] vectorList;
    }

    public class JEnvelopeVector
    {
        public JEnvelopeVectorMode mode;
        public short time;
        public short value;
        public JEnvelopeVector next;
    }

    public enum JEnvelopeVectorMode
    {
        Linear = 0,
        Square = 1,
        SquareRoot = 2,
        SampleCell = 3,

        Loop = 13,
        Hold = 14,
        Stop = 15,
    }
}
