using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JaiSeqX.JAI.Seq
{
    public partial class JAISeqSubroutine
    {
        public JAISeqEvent ProcessArithmeticOps(byte currnet_opcode)
        {
            switch (currnet_opcode)
            {
                /* ARITHMATIC OPERATORS */
                case (byte)JAISeqEvent.ADDR:
                    {
                        var destination_reg = Sequence.ReadByte();
                        var source_reg = Sequence.ReadByte();
                        rI[0] = destination_reg;
                        rI[1] = source_reg;
                        rI[2] = (Registers[destination_reg] += Registers[source_reg]);
                        return JAISeqEvent.ADDR;
                    }
                case (byte)JAISeqEvent.MULR:
                    {
                        var destination_reg = Sequence.ReadByte();
                        var source_reg = Sequence.ReadByte();
                        rI[0] = destination_reg;
                        rI[1] = source_reg;
                        rI[2] = (Registers[destination_reg] *= Registers[source_reg]);
                        return JAISeqEvent.MULR;
                    }
                case (byte)JAISeqEvent.CMPR:
                    {
                        var destination_reg = Sequence.ReadByte();
                        var source_reg = Sequence.ReadByte();
                        rI[0] = destination_reg;
                        rI[1] = source_reg;
                        rI[2] = compare(Registers[source_reg], Registers[destination_reg]);
                        return JAISeqEvent.CMPR;
                    }
                case (byte)JAISeqEvent.ADD8:
                    {
                        var destination_reg = Sequence.ReadByte();
                        var value = Sequence.ReadByte();
                        rI[0] = destination_reg;
                        rI[1] = value;
                        rI[2] = (Registers[destination_reg] += value);
                        return JAISeqEvent.ADD8;
                    }

                case (byte)JAISeqEvent.MUL8:
                    {
                        var destination_reg = Sequence.ReadByte();
                        var value = Sequence.ReadByte();
                        rI[0] = destination_reg;
                        rI[1] = value;
                        rI[2] = (Registers[destination_reg] *= value);
                        return JAISeqEvent.MUL8;
                    }

                case (byte)JAISeqEvent.CMP8:
                    {
                        var destination_reg = Sequence.ReadByte();
                        var value = Sequence.ReadByte();
                        rI[0] = destination_reg;
                        rI[1] = value;
                        rI[2] = compare(Registers[destination_reg], value);
                        return JAISeqEvent.CMP8;
                    }

                case (byte)JAISeqEvent.ADD16:
                    {
                        var destination_reg = Sequence.ReadByte();
                        var value = Sequence.ReadInt16();
                        rI[0] = destination_reg;
                        rI[1] = value;
                        rI[2] = (Registers[destination_reg] += value);
                        return JAISeqEvent.ADD16;
                    }

                case (byte)JAISeqEvent.MUL16:
                    {
                        var destination_reg = Sequence.ReadByte();
                        var value = Sequence.ReadByte();
                        rI[0] = destination_reg;
                        rI[1] = value;
                        rI[2] = (Registers[destination_reg] *= value);
                        return JAISeqEvent.MUL16;
                    }

                case (byte)JAISeqEvent.CMP16:
                    {
                        var destination_reg = Sequence.ReadByte();
                        var value = Sequence.ReadByte();
                        rI[0] = destination_reg;
                        rI[1] = value;
                        rI[2] = compare(Registers[destination_reg], value);
                        return JAISeqEvent.CMP16;
                    }
           
            }

            return JAISeqEvent.UNKNOWN;
        }
    }
}