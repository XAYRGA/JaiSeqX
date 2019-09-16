using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;
using System.IO;

namespace JaiSeqX.JAI.Seq
{

   
    
    public class JAISeqSubroutine
    {
       
        byte[] SeqData; // Full .BMS  file.  
        BeBinaryReader Sequence; // Reader for SeqData
        private int baseAddress; // The address at which this subroutine starts in the file.
        public byte last_opcode; // The last opcode that was executed. 
        public JAISeqRegisterMap Registers;  // JAISeq Registers.
        public JAISeqRegisterMap Ports;  //  Ports, for ReadPort and WritePort -- used for interfacing with external data or game events.
        public Stack<int> AddrStack; // JAISeq return stack, depth of 8, used for CALL and RETURN commands.
        public Queue<JAISeqExecutionFrame> history; // Execution history.  
        public int[] rI; // Internal Integer registers  -- for interfacing with sequence. 
        public float[] rF; // Internal Float registers -- for interfacing with sequence.
        public int pc // Current Program Counter
        {
            get
            {
                return (int)Sequence.BaseStream.Position;
            }
            set
            {
                Sequence.BaseStream.Position = value;
            }
        }
        public int pcl;
        public JAISeqSubroutine(ref byte[] BMSData,int BaseAddr)
        {
            SeqData = BMSData; // 
            AddrStack = new Stack<int>(8); // JaiSeq has a stack depth of 8
            history = new Queue<JAISeqExecutionFrame>(16); // Ill keep an opcode depth of 16      
            Sequence = new BeBinaryReader(new MemoryStream(BMSData)); // Make a reader for this. 
            Sequence.BaseStream.Position = BaseAddr; // Set its position to the base address. 
            baseAddress = BaseAddr; // store the base address
            rI = new int[8]; 
            rF = new float[8];
        }


        private void skip(int bytes)
        {
            Sequence.BaseStream.Seek(bytes, SeekOrigin.Current); // Tells the sequence to seek elsewhere. 
        }

        private void reset()
        {
            Sequence.BaseStream.Position = baseAddress; // Seek Back to base address
        }

        public void jump(int pos)
        {
            Sequence.BaseStream.Position = pos; // move to new address
        }

        private bool checkCondition(byte cond)
        {
            var conditionValue = Registers[3]; 
            // Explanation:
            // When a compare function is executed. the registers are subtracted. 
            // The subtracted result is stored in R3. 
            // This means:
        
            switch (cond)
            {
                case 0: // We were probably given the wrong commmand
                    return true;  // oops, all boolean
                case 1: // Equal, if r1 - r2 == 0, then they obviously both had the same value
                    if (conditionValue==0) { return true; }
                    return false; 
                case 2: // Not Equal, If r1 - r2 doesn't equal 0, they were not the same value.
                    if (conditionValue!=0) { return true; }
                    return false; 
                case 3: // One good question.
                    if (conditionValue==1) { return true; }
                    return false;
                case 4: // Less Than if r1 - r2 is more than zero, this means r1 was less than r2
                    if (conditionValue > 0) { return true; }
                    return false;
                case 5: // Greater than , if r1 - r2 is less than 0, that means r1 was bigger than r2
                    if (conditionValue < 0) { return true; }
                    return false; 
            }
            return false;
        }

        public short compare(short a, short b)
        {
            short res = (short)(a - b);
            Registers[3] = res;
            return res;
        }
        public JAISeqEvent loadNextOp()
        {
            if (history.Count == 16)  // Opstack is full
                history.Dequeue(); // push the one off the end. 
            var historyPos = (int)Sequence.BaseStream.Position; // store push address for FIFO stack. 
            pcl = (int)Sequence.BaseStream.Position; // Store the last known program counter.  
            byte current_opcode = Sequence.ReadByte(); // Reads the current byte in front of the cursor. 
            last_opcode = current_opcode; // Store last opcode 
            history.Enqueue(new JAISeqExecutionFrame { opcode = last_opcode,  address = historyPos} ); // push opcode to FIFO stack

            if (current_opcode < 0x80)  // anything 0x80  or under is a NOTE_ON, this lines up with MIDI notes.
            {
                rI[0] = current_opcode; // The note on event is laid out like a piano with 127 (0x7F1) keys. 
                // So this means that the first 0x80 bytes are just pressing the individual keys.
                rI[1] = Sequence.ReadByte(); // The next byte tells the voice, 0-8
                rI[2] = Sequence.ReadByte(); // And finally, the next byte will tell the velocity 
                return JAISeqEvent.NOTE_ON; // Return the note on event. 

            } else if (current_opcode==(byte)JAISeqEvent.WAIT_8) // Contrast to above, the opcode between these two is WAIT_U8
            {
                rI[0] = Sequence.ReadByte(); // Add u8 ticks to the delay.  
                return JAISeqEvent.WAIT_8;
            } else if (current_opcode < 0x88) // We already check if it's 0x80, so anything between here will be 0x81 and 0x87
            {
                
                rI[0] = (byte)(current_opcode & 0x7F); // Only the first 7 bits are going to determine which voice we're stopping. 
                return JAISeqEvent.NOTE_OFF;
            } else // Finally, we can fall into our CASE statement. 
            {
                switch (current_opcode)
                {
                    /* Delays and waits */
                    case (byte)JAISeqEvent.WAIT_16: // Wait (UInt16)
                        {
                            var delay = Sequence.ReadUInt16(); // load delay into ir0                  
                            rI[0] = delay;
                            return JAISeqEvent.WAIT_16;
                        }
                    case (byte)JAISeqEvent.WAIT_VAR: // Wait (VLQ) see readVlq function. 
                        {
                            var delay = Helpers.ReadVLQ(Sequence); // load delay into ir0
                            rI[0] = delay; 
                            return JAISeqEvent.WAIT_VAR;
                        }
                    /* Logical jumps */ 
                    case (byte)JAISeqEvent.JUMP: // Unconditional jump
                        {
                            rI[0] = 0; // No condition, r0
                            var addr = Sequence.ReadInt32(); // Absolute address r1
                            jump(addr);
                            rI[1] = addr;
                            return JAISeqEvent.JUMP;
                        }
                    case (byte)JAISeqEvent.JUMP_CONDITIONAL: // Jump based on mode
                        {
                            byte flags = Sequence.ReadByte(); // Read flags.
                            var condition = flags & 15; // last nybble is condition. 
                            rI[0] = flags;  // store flags in ir0        
                            rI[1] = 0;
                            var addr = (int)Helpers.ReadUInt24BE(Sequence); // pointer, push to ir1
                            var yesJump = checkCondition((byte)condition); // Should we actually jump?
                            if (yesJump)
                            {
                                jump(addr); // Jump to the specified position.
                                rI[1] = addr;
                            }
                            return JAISeqEvent.JUMP_CONDITIONAL;
                        }
                    case (byte)JAISeqEvent.RETURN_CONDITIONAL:
                        {
                            var cond = Sequence.ReadByte(); // Read condition byte
                            var cCheck = checkCondition(cond); // Check the condition register
                            rI[0] = cond; // Store the condition in ir0
                            rI[1] = 0; // clear address register
                            if (cCheck)
                            {
                                var returnTo = AddrStack.Pop(); // Pull from address stack
                                jump(returnTo);
                                rI[1] = returnTo; // set address register
                            }
                            return JAISeqEvent.RETURN_CONDITIONAL;
                        }
                    case (byte)JAISeqEvent.CALL_CONDITIONAL:
                        {
                            var cond = Sequence.ReadByte();
                            var addr = (int)Helpers.ReadUInt24BE(Sequence);
                            var cCheck = checkCondition(cond);
                            rI[0] = cond; // Set to condition
                            rI[1] = 0;  // clear address register
                            if (cCheck)
                            {
                                AddrStack.Push(pc); // Push to address stack
                                jump(addr); // Jup to specified address
                                rI[1] = addr; // set ir1 to address jumped to
                            }
                            return JAISeqEvent.CALL;
                        }
                    case (byte)JAISeqEvent.RETURN:
                        {
                            return JAISeqEvent.RETURN;
                        }
                    /* PORT COMANDS */
                    case (byte)JAISeqEvent.READPORT:
                        {
                            var port_id = Sequence.ReadByte(); // Read Port ID
                            var dest_reg = Sequence.ReadByte(); //  Read Destination Register
                            rI[0] = port_id; // push id to ir0
                            rI[1] = dest_reg; // push register to ir1
                            Registers[dest_reg] = Ports[port_id]; // Do move operation
                            return JAISeqEvent.READPORT;
                        }
                    /* Tempo Control */
                    case (byte)JAISeqEvent.J2_SET_ARTIC: // The very same.
                        {
                            var type = Sequence.ReadByte();
                            var val = Sequence.ReadInt16();
                            rI[0] = type; // 0x62 is  tempo, tho. 
                            rI[1] = val;                          
                            return JAISeqEvent.J2_SET_ARTIC;
                        }
                    case (byte)JAISeqEvent.TIME_BASE: // Set ticks per quarter note.
                        {
                            rI[0] = (short)(Sequence.ReadInt16());
                            Console.WriteLine("Timebase ppqn set {0}", rI[0]);
                            return JAISeqEvent.TIME_BASE;
                        }
                    case (byte)JAISeqEvent.J2_TEMPO: // Set BPM, Same format
                    case (byte)JAISeqEvent.TEMPO: // Set BPM
                        {
                            rI[0] = (short)(Sequence.ReadInt16());
                            return JAISeqEvent.TIME_BASE;
                        }
                    /* Track Control */

                    case (byte)JAISeqEvent.OPEN_TRACK:
                        {
                            rI[0] = Sequence.ReadByte();
                            rI[1] = (int)Helpers.ReadUInt24BE(Sequence);  // Pointer to track inside of BMS file (Absolute) 
                            return JAISeqEvent.OPEN_TRACK;
                        }
                    case (byte)JAISeqEvent.FIN:
                        {
                            return JAISeqEvent.FIN;
                        }
                    case (byte)JAISeqEvent.J2_SET_BANK:
                        {
                            rI[0] = Sequence.ReadByte();
                            return JAISeqEvent.J2_SET_BANK;
                        }
                    case (byte)JAISeqEvent.J2_SET_PROG:
                        {
                            rI[0] = Sequence.ReadByte();
                            return JAISeqEvent.J2_SET_PROG;
                        }

                    /* Parameter control */
                    case (byte)JAISeqEvent.J2_SET_PARAM_8:
                        {
                            rI[0] = Sequence.ReadByte();
                            rI[1] = Sequence.ReadByte();
                            Registers[(byte)rI[0]] = (short)rI[1];
                            return JAISeqEvent.J2_SET_PARAM_8;
                        }
                    case (byte)JAISeqEvent.J2_SET_PARAM_16:
                        {
                            rI[0] = Sequence.ReadByte();
                            rI[1] = Sequence.ReadInt16();
                            Registers[(byte)rI[0]] = (short)rI[1];
                            return JAISeqEvent.J2_SET_PARAM_16;
                        }
                    case (byte)JAISeqEvent.PARAM_SET_R:
                        {
                            var register1 = Sequence.ReadByte();
                            var register2 = Sequence.ReadByte();
                            rI[0] = register1;
                            rI[2] = register2;
                            Registers[register1] = Registers[register2];
                            return JAISeqEvent.PARAM_SET_R;
                        }
                    case (byte)JAISeqEvent.PARAM_SET_8: // Set track parameters (Usually used for instruments)
                        {
                            var reg = Sequence.ReadByte();
                            var val = Sequence.ReadByte();
                            rI[0] = reg;
                            rI[1] = val;
                            Registers[reg] = val;
                            return JAISeqEvent.PARAM_SET_8;
                        }
                    case (byte)JAISeqEvent.PARAM_SET_16: // Set track parameters (Usually used for instruments)
                        {
                            var reg = Sequence.ReadByte();
                            var val = Sequence.ReadInt16();
                            rI[0] = reg;
                            rI[1] = val;
                            Registers[reg] = val;                    
                            return JAISeqEvent.PARAM_SET_16;
                        }
                    case (byte)JAISeqEvent.PRINTF:
                        {
                            var lastread = -1;
                            string v = "";
                            while (lastread != 0)
                            {
                                lastread = Sequence.ReadByte();
                                v += (char)lastread;
                            }
                            // Sequence.ReadByte();

                            return JAISeqEvent.UNKNOWN;
                        }
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
                            rF[0] = ((float)value/ 0xFF);
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
                            rI[2] = compare(Registers[destination_reg],value);
                            return JAISeqEvent.CMP8;
                        }

                    case (byte)JAISeqEvent.ADD16:
                        {
                            var destination_reg = Sequence.ReadByte();
                            var value = Sequence.Reade();
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
                    /* Unsure as of yet, but we have to keep alignment */
                    case 0xE7:
                       skip(2);
                        // Console.WriteLine(Sequence.ReadByte());
                        //Console.WriteLine(Sequence.ReadByte());

                        return JAISeqEvent.UNKNOWN;
                    case 0xDD:
                    case 0xED:
                        skip(3);
                        return JAISeqEvent.UNKNOWN;
                    case 0xEF:
                    case 0xF9:
                    case 0xE6:
                        skip(2);
                        return JAISeqEvent.UNKNOWN;
                    case 0xA9:
                        skip(4);
                        return JAISeqEvent.UNKNOWN;
                    case 0xAA:
                        skip(4);
                        return JAISeqEvent.UNKNOWN;
                    case 0xAD:
                       // State.delay += 0xFFFF;
                       // Add (byte) register.  + (short) value
                       // 
                        skip(3);
                        return JAISeqEvent.UNKNOWN;
                    case 0xAE:                        
                        return JAISeqEvent.UNKNOWN;
                    case 0xB1:
                    case 0xB2:
                    case 0xB3:
                    case 0xB4:
                    case 0xB5:
                    case 0xB6:
                    case 0xB7:
                    int flag = Sequence.ReadByte();
                        if (flag == 0x40) { skip(2); }
                        if (flag == 0x80) { skip(4); }
                        return JAISeqEvent.UNKNOWN;
                    case 0xDB:
                   
                    case 0xDF:
                    
                        skip(4);
                        return JAISeqEvent.UNKNOWN;
                    case 0xBE:
                        skip(2);
                        return JAISeqEvent.UNKNOWN;
                    case 0xCC:
                        skip(2);
                        return JAISeqEvent.UNKNOWN;
                    case 0xCF:
                        skip(1);
                        return JAISeqEvent.UNKNOWN;
                    case 0xD0:
                    case 0xD1:
                    case 0xD2:
                    case 0xD5:
                    case 0xD9:

                    case 0xDE:
                    case 0xDA:
                   
                        skip(1);
                        return JAISeqEvent.UNKNOWN;
                    case 0xF1:
                    case 0xF4:

                    case 0xD6:
                        skip(1);
                        //Console.WriteLine(Sequence.ReadByte());
                        return JAISeqEvent.UNKNOWN;
                    case 0xBC:
                        return JAISeqEvent.UNKNOWN;
                }
            }
            return JAISeqEvent.MISS; // ABSOLUTE FUCKING DEATH. 
        }



    }
}
