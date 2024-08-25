using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libJAudio.Sequence.Inter
{
    public partial class JAISeqInterpreter
    {
        public JAISeqEvent ProcessArithmeticOps(byte currnet_opcode)
        {
            switch (currnet_opcode)
            {
                /* ARITHMATIC OPERATORS */
                case 0xA1: // ADDR
                    {
                        var destination_reg = Sequence.ReadByte();
                        var source_reg = Sequence.ReadByte();
                        rI[0] = destination_reg;
                        rI[1] = source_reg;
                        return JAISeqEvent.ADDR;
                    }
                case 0xA2: // MULR
                    {
                        var destination_reg = Sequence.ReadByte();
                        var source_reg = Sequence.ReadByte();
                        rI[0] = destination_reg;
                        rI[1] = source_reg;
                        return JAISeqEvent.MULR;
                    }
                case 0xA3: // CMPR
                    {
                        var destination_reg = Sequence.ReadByte();
                        var source_reg = Sequence.ReadByte();
                        rI[0] = destination_reg;
                        rI[1] = source_reg;
                        return JAISeqEvent.CMPR;
                    }
                case 0xA5: // ADD8
                    {
                        var destination_reg = Sequence.ReadByte();
                        var value = Sequence.ReadByte();
                        rI[0] = destination_reg;
                        rI[1] = value;
                        return JAISeqEvent.ADD8;
                    }

                case 0xA6: // MUL8
                    {
                        var destination_reg = Sequence.ReadByte();
                        var value = Sequence.ReadByte();
                        rI[0] = destination_reg;
                        rI[1] = value;
                        return JAISeqEvent.MUL8;
                    }

                case 0xA7: // CMP8
                    {
                        var destination_reg = Sequence.ReadByte();
                        var value = Sequence.ReadByte();
                        rI[0] = destination_reg;
                        rI[1] = value;
                        return JAISeqEvent.CMP8;
                    }
                case 0xAA:
                    {
                      
                        var mode = Sequence.ReadByte();
                        if (mode == 0x20)
                        {
                            rI[0] = mode;
                            var dest_register = Sequence.ReadByte();
                            var address_register = Sequence.ReadByte();
                            var relative_register = Sequence.ReadByte();
                            rI[1] = dest_register;
                            rI[2] = address_register;
                            rI[3] = relative_register;
                        }
                        return JAISeqEvent.LOADTBL;
                    }
                case 0xAD: // ADD16
                    {
                        var destination_reg = Sequence.ReadByte();
                        var value = Sequence.ReadInt16();
                        //Console.WriteLine($"VALUE IS {value}");
                        rI[0] = destination_reg;
                        rI[1] = value;
                        return JAISeqEvent.ADD16;
                    }

                case 0xAE: // MUL16
                    {
                        var destination_reg = Sequence.ReadByte();
                        var value = Sequence.ReadByte();
                        rI[0] = destination_reg;
                        rI[1] = value;
                        return JAISeqEvent.MUL16;
                    }

                case 0xAF: // CMP16                   
                    {
                        var destination_reg = Sequence.ReadByte();
                        var value = Sequence.ReadByte();
                        rI[0] = destination_reg;
                        rI[1] = value;
                        return JAISeqEvent.CMP16;
                    }
            }

            return JAISeqEvent.UNKNOWN;
        }
    }
}