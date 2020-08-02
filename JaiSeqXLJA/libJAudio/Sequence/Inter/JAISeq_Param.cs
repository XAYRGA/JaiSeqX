using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libJAudio.Sequence.Inter
{
    public partial class JAISeqInterpreter
    {
        public JAISeqEvent ProcessParamOps(byte currnet_opcode)
        {
            switch (currnet_opcode)
            {
                /* PORT COMANDS */
                case 0xCB: // READPORT
                    {
                        var port_id = Sequence.ReadByte(); // Read Port ID
                        var dest_reg = Sequence.ReadByte(); //  Read Destination Register
                        rI[0] = port_id; // push id to ir0
                        rI[1] = dest_reg; // push register to ir1
                        return JAISeqEvent.READPORT;
                    }

                case 0xCC:
                    {
                        var port_id = Sequence.ReadByte();
                        var source_reg = Sequence.ReadByte();
                        rI[0] = port_id;
                        rI[1] = source_reg;
                        return JAISeqEvent.WRITEPORT;
                    }
                case 0xD1: // Write Parent Port 
                    {
                        var port_id = Sequence.ReadByte();
                        var source_reg = Sequence.ReadByte();
                        rI[0] = port_id;
                        rI[1] = source_reg;
                        return JAISeqEvent.WRITE_PARENT_PORT;
                    }
                /* Tempo Control */
                case 0xD8: // The very same.
                    {
                        //Console.WriteLine(InterpreterVersion);
                        if (InterpreterVersion == JAISeqInterpreterVersion.JA1) {

                            Sequence.ReadInt32(); // skip 4 bytes
                            return JAISeqEvent.SIMPLE_ADSR;
                        } else
                        {
                            var type = Sequence.ReadByte();
                            var val = Sequence.ReadInt16();
                            rI[0] = type; // 0x62 is  tempo, tho. 
                            rI[1] = val;
                            return JAISeqEvent.J2_SET_ARTIC;
                        }
                       
                    }
                case 0xFD: // (v1)TIME_BASE (v2)J2_PRINTF TODOTODO
                    {
                        if (InterpreterVersion == JAISeqInterpreterVersion.JA1) { 
                            rI[0] = (short)(Sequence.ReadInt16());
                            return JAISeqEvent.TIME_BASE;
                        }
                        if (InterpreterVersion == JAISeqInterpreterVersion.JA2)
                        {
                            var lastread = -1;
                            string v = "";
                            while (lastread != 0)
                            {
                                lastread = Sequence.ReadByte();
                                v += (char)lastread;
                            }
                            Console.WriteLine(v);
                            var l = Sequence.ReadByte();
                            if (l != 0)
                                Sequence.BaseStream.Position = Sequence.BaseStream.Position - 1;
                            return JAISeqEvent.J2_PRINTF;
                        }
                        return JAISeqEvent.UNKNOWN;
                    }
                case 0xE0: // J2_TEMPO (v2)
                case 0xFE: // TEMPO (v1)
                    {
                        rI[0] = (short)(Sequence.ReadInt16());
                        return JAISeqEvent.TEMPO;
                    }
                /* Parameter control */
                case 0xB8: // J2_SET_PARAM_8
                    {
                        rI[0] = Sequence.ReadByte();
                        rI[1] = Sequence.ReadByte();
                        return JAISeqEvent.J2_SET_PARAM_8;
                    }
                case 0xB9: // J2_SET_PARAM_16
                    {
                        rI[0] = Sequence.ReadByte();
                        rI[1] = Sequence.ReadInt16();
                        return JAISeqEvent.J2_SET_PARAM_16;
                    }
                case 0xA0: // PARAM_SET_R
                    {
                        var register1 = Sequence.ReadByte();
                        var register2 = Sequence.ReadByte();
                        rI[0] = register1;
                        rI[2] = register2;
                        return JAISeqEvent.PARAM_SET_R;
                    }
                case 0xA4: //  PARAM_SET_8
                    {
                        var reg = Sequence.ReadByte();
                        var val = Sequence.ReadByte();
                        rI[0] = reg;
                        rI[1] = val;
                        return JAISeqEvent.PARAM_SET_8;
                    }
                case 0xAC: // PARAM_SET_16
                    {
                        var reg = Sequence.ReadByte();
                        var val = Sequence.ReadInt16();
                        rI[0] = reg;
                        rI[1] = val;
                        return JAISeqEvent.PARAM_SET_16;
                    }
                case 0xE2: //J2_SET_BANK:
                    {
                        rI[0] = Sequence.ReadByte();
                        return JAISeqEvent.J2_SET_BANK;
                    }
                case 0xE3: //J2_SET_PROG
                    {
                        rI[0] = Sequence.ReadByte();
                        return JAISeqEvent.J2_SET_PROG;
                    }

            }
            return JAISeqEvent.UNKNOWN;
        }
    }
}