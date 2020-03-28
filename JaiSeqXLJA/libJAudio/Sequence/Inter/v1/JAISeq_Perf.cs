using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace libJAudio.Sequence.Inter
{
    public partial class JAISeqInterpreter
    {
        public JAISeqEvent ProcessPerfOps(byte currnet_opcode)
        {
            switch (currnet_opcode)
            {
                /* PERF Control*/
                /* Perf structure is as follows
                 * <byte> type 
                 * <?> val
                 * (<?> dur)
                */
                case (byte)JAISeqEvent.PERF_U8_NODUR:
                    {
                        var perf = Sequence.ReadByte();
                        var value = Sequence.ReadByte();
                        // Registers[perf] = value; // MMAYBE NOT?
                        rI[0] = perf;
                        rI[1] = value;
                        rI[2] = 0;
                        rF[0] = (float)value / 0xFF;
                        return JAISeqEvent.PERF_U8_NODUR;
                    }
                case (byte)JAISeqEvent.PERF_U8_DUR_U8:
                    {
                        var perf = Sequence.ReadByte();
                        var value = Sequence.ReadByte();
                        var delay = Sequence.ReadByte();
                        rI[0] = perf;
                        rI[1] = value;
                        rI[2] = delay;
                        rF[0] = ((float)value / 0xFF);
                        return JAISeqEvent.PERF_U8_DUR_U8;
                    }
                case (byte)JAISeqEvent.PERF_U8_DUR_U16:
                    {
                        var perf = Sequence.ReadByte();
                        var value = Sequence.ReadByte();
                        var delay = Sequence.ReadUInt16();
                        rI[0] = perf;
                        rI[1] = value;
                        rI[2] = delay;
                        rF[0] = ((float)value / 0xFF);
                        return JAISeqEvent.PERF_U8_DUR_U16;
                    }
                case (byte)JAISeqEvent.PERF_S8_NODUR:
                    {
                        var perf = Sequence.ReadByte();
                        var value = Sequence.ReadByte();
                        // Registers[perf] = value; // MMAYBE NOT?
                        rI[0] = perf;
                        rI[1] = (value > 0x7F) ? value - 0xFF : value;
                        rI[2] = 0;
                        rF[0] = ((float)(rI[1]) / 0x7F);
                        return JAISeqEvent.PERF_S8_NODUR;
                    }
                case (byte)JAISeqEvent.PERF_S8_DUR_U8:
                    {
                        var perf = Sequence.ReadByte();
                        var value = Sequence.ReadByte();
                        var delay = Sequence.ReadByte();
                        rI[0] = perf;
                        rI[1] = (value > 0x7F) ? value - 0xFF : value;
                        rI[2] = delay;
                        rF[0] = ((float)(rI[1]) / 0x7F);
                        return JAISeqEvent.PERF_S8_DUR_U8;
                    }

                case (byte)JAISeqEvent.PERF_S8_DUR_U16:
                    {
                        var perf = Sequence.ReadByte();
                        var value = Sequence.ReadByte();
                        var delay = Sequence.ReadUInt16();
                        rI[0] = perf;
                        rI[1] = (value > 0x7F) ? value - 0xFF : value;
                        rI[2] = delay;
                        rF[0] = ((float)(rI[1]) / 0x7F);
                        return JAISeqEvent.PERF_S8_DUR_U16;
                    }

                case (byte)JAISeqEvent.PERF_S16_NODUR:
                    {
                        var perf = Sequence.ReadByte();
                        var value = Sequence.ReadInt16();
                        rI[0] = perf;
                        rI[1] = value;
                        rF[0] = ((float)(value) / 0x7FFF);
                        return JAISeqEvent.PERF_S16_NODUR;
                    }

                case (byte)JAISeqEvent.PERF_S16_DUR_U8:
                    {
                        var perf = Sequence.ReadByte();
                        var value = Sequence.ReadInt16();
                        var delay = Sequence.ReadByte();
                        rI[0] = perf;
                        rI[1] = value;
                        rI[2] = delay;
                        rF[0] = ((float)(value) / 0x7FFF);
                        return JAISeqEvent.PERF_S16_DUR_U8;
                    }
                case (byte)JAISeqEvent.PERF_S16_DUR_U16:
                    {
                        var perf = Sequence.ReadByte();
                        var value = Sequence.ReadInt16();
                        var delay = Sequence.ReadUInt16();
                        rI[0] = perf;
                        rI[1] = value;
                        rI[2] = delay;
                        rF[0] = ((float)(value) / 0x7FFF);
                        return JAISeqEvent.PERF_S16_DUR_U16;
                    }
            }
            return JAISeqEvent.UNKNOWN;
        }
    }
}
