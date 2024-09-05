using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libJAudio.Sequence.Inter
{
    public partial class JAISeqInterpreter
    {
        public JAISeqEvent ProcessFlowOps(byte currnet_opcode)
        {

            switch (currnet_opcode)
            {

                /* Open and close track */
                case 0xC1: // OPEN_TRACK
                    {
                        rI[0] = Sequence.ReadByte();
                        rI[1] = (int)Helpers.ReadUInt24BE(Sequence);  // Pointer to track inside of BMS file (Absolute) 
                        return JAISeqEvent.OPEN_TRACK;
                    }
                case 0xC2: // OPEN_TRACK_BROS
                    var w = Sequence.ReadByte();
                    rI[0] = w;
                    if (InterpreterVersion == JAISeqInterpreterVersion.JA2)
                    {
                      
                        return JAISeqEvent.J2_CLOSE_TRACK;
                    }
                   
                    return JAISeqEvent.OPEN_TRACK_BROS;
                case 0xC9:
                    return JAISeqEvent.LOOPS;
                case 0xCA:
                    return JAISeqEvent.LOOPE;
                case 0xFF: // FIN
                    {
                        return JAISeqEvent.FIN;
                    }
                /* Delays and waits */
                case 0x88: // WAIT_16 (UInt16)
                    {
                        var delay = Sequence.ReadUInt16(); // load delay into ir0                  
                        rI[0] = delay;
                        return JAISeqEvent.WAIT_16;
                    }
                case 0xF0: // WAIT_VAR
                    {
                        var delay = Helpers.ReadVLQ(Sequence); // load delay into ir0
                        rI[0] = delay;
                        return JAISeqEvent.WAIT_VAR;
                    }
                case 0xCF: // WAIT_REGISTER
                    {
                        var register = Sequence.ReadByte();
                        rI[0] = register;
                        return JAISeqEvent.WAIT_REGISTER;
                    }
                /* Logical jumps */
                case 0xC7: // JUMP
                    {
                        rI[0] = 0; // No condition, r0
                        var addr = Helpers.ReadUInt24BE(Sequence); // Absolute address r1
                        //jump(addr);
                        rI[1] = (int)addr;
                        return JAISeqEvent.JUMP;
                    }
                case 0xC8: // JUMP_CONDITIONAL
                    {
                        byte flags = Sequence.ReadByte(); // Read flags.
                        var condition = flags & 15; // last nybble is condition. 
                        var addr = (int)Helpers.ReadUInt24BE(Sequence); // pointer, push to ir1
                        rI[0] = flags;
                        rI[1] = addr;
                        rI[2] = condition;
                        return JAISeqEvent.JUMP_CONDITIONAL;
                    }
                case 0xC6: // RETURN_CONDITIONAL
                    {
                        var cond = Sequence.ReadByte(); // Read condition byte
                        rI[0] = cond; // Store the condition in ir0
                        return JAISeqEvent.RETURN_CONDITIONAL;
                    }
                case 0xC4: // CALL_CONDITIONAL
                    {
                        var cond = Sequence.ReadByte();

                        var reg = 0;
                        if (cond==0xC0)
                            reg = Sequence.ReadByte();

                        var addr = (int)Helpers.ReadUInt24BE(Sequence);
                 
                        rI[0] = cond; // Set to condition
                        rI[1] = addr; // set ir1 to address jumped
                        rI[2] = reg;
                        return JAISeqEvent.CALL_CONDITIONAL;
                    }
                case 0xF3:
                    {

                        if (InterpreterVersion == JAISeqInterpreterVersion.JA1)
                            skip(3);
                        else
                        {
                            rI[0] = (int)Helpers.ReadUInt24BE(Sequence);
                            return JAISeqEvent.J2_JMPTBL;
                        }

                        return JAISeqEvent.VOLUME_MODE;
                    }
                case 0xC3: // CALL
                    {
                        var addr = (int)Helpers.ReadUInt24BE(Sequence);
                        rI[0] = addr; // Set address
                        return JAISeqEvent.CALL;
                    }
                case 0xC5: // RETURN
                    {
                        return JAISeqEvent.RETURN;
                    }
            
                case 0xB1:
                    {
                        rI[0] = Sequence.ReadByte(); // Instruction 
                        rI[1] = Sequence.ReadByte(); // Override mask 
                        rI[2] = Sequence.ReadByte(); // Data 1(WRONG) 
                        rI[3] = Sequence.ReadByte(); // Data Register
                        return JAISeqEvent.OVERRIDE_1;
                    }
                case 0x91:
                    {
                        rI[0] = Sequence.ReadByte(); // Mask 
                        rI[1] = Sequence.ReadByte(); // Instruction
                        rI[2] = Sequence.ReadByte(); // Data 1(WRONG) 
                        rI[3] = Sequence.ReadByte(); // Data Register
                        return JAISeqEvent.J2_OVERRIDE;
                    }
            }
            return JAISeqEvent.UNKNOWN;
        }
    }
}
