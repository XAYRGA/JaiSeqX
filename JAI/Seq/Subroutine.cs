using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;
using System.IO;

namespace JaiSeqX.JAI.Seq
{

    public enum JaiSeqEvent
    {

        /* wait with u8 arg */
        WAIT_8 = 0x80,
        /* wait with u16 arg */
        WAIT_16 = 0x88,
        /* wait with variable-length arg */
        WAIT_VAR = 0xF0,

        /* Perf is hard to enum, but here goes: */
        PERF_U8_NODUR = 0x94,
        PERF_U8_DUR_U8 = 0x96,
        PERF_U8_DUR_U16 = 0x97,
        PERF_S8_NODUR = 0x98,
        PERF_S8_DUR_U8 = 0x9A,
        PERF_S8_DUR_U16 = 0x9B,
        PERF_S16_NODUR = 0x9C,
        PERF_S16_DUR_U8 = 0x9E,
        PERF_S16_DUR_U16 = 0x9F,

        PARAM_SET_8 = 0xA4,
        PARAM_SET_16 = 0xAC,

        OPEN_TRACK = 0xC1,

        /* open sibling track, seems unused */
        OPEN_TRACK_BROS = 0xC2,
        CALL = 0xC3,
        CALL_COND = 0xC4,
        RET = 0xC5,
        RET_COND = 0xC6,
        JUMP = 0xC7,
        JUMP_COND = 0xC8,

      

        TIME_BASE = 0xFD,
        TEMPO = 0xFE,
        FIN = 0xFF,

        /* "Improved" JaiSeq from TP / SMG / SMG2 seems to use this instead */
        J2_SET_PERF_8 = 0xB8,
        J2_SET_PERF_16 = 0xB9,
        /* Set "articulation"? Used for setting timebase. */
        J2_SET_ARTIC = 0xD8,
        J2_TEMPO = 0xE0,
        J2_SET_BANK = 0xE2,
        J2_SET_PROG = 0xE3,
    }


    public class Subroutine
    {
        BeBinaryReader Sequence;
        byte[] SeqData;

        Stack<uint> AddrStack;

        public Queue<byte> OpcodeHistory;
        public Queue<int> OpcodeAddressStack;
        
   
        private int baseAddress;

        public JSequenceState State; 

        public Subroutine(ref byte[] BMSData,int BaseAddr)
        {
            State = new JSequenceState();
            SeqData = BMSData;

            AddrStack = new Stack<uint>(64); // 64 unit stack? 
            OpcodeHistory = new Queue<byte>(16);
            OpcodeAddressStack = new Queue<int>(16);

            Sequence = new BeBinaryReader(new MemoryStream(BMSData));
            Sequence.BaseStream.Position = BaseAddr;

            State = new JSequenceState();

            baseAddress = BaseAddr;
          

        }


        private void skip(int bytes)
        {
            Sequence.BaseStream.Seek(bytes, SeekOrigin.Current);
        }

        private void reset()
        {
            Sequence.BaseStream.Position = baseAddress;
        }

        public void jump(int pos)
        {
            Sequence.BaseStream.Position = pos;
        }

        public JaiEventType loadNextOp()
        {
            if (OpcodeAddressStack.Count == 16)
            {
                OpcodeAddressStack.Dequeue();
            }
            if (OpcodeHistory.Count == 16)
            {
                OpcodeHistory.Dequeue(); 
            }

            OpcodeAddressStack.Enqueue((int)Sequence.BaseStream.Position); // push address to FIFO stack. 

            State.current_address = (int)Sequence.BaseStream.Position;
            byte current_opcode = Sequence.ReadByte(); // Reads the current byte in front of the cursor. 

            OpcodeHistory.Enqueue(current_opcode); // push opcode to FIFO stack

           
            
            if (current_opcode < 0x80)
            {
                State.note = current_opcode; // The note on event is laid out like a piano with 127 (0x7F1) keys. 
                // So this means that the first 0x80 bytes are just pressing the individual keys.
                State.voice = Sequence.ReadByte(); // The next byte tells the voice, 0-8
                State.vel = Sequence.ReadByte(); // And finally, the next byte will tell the velocity 
                return JaiEventType.NOTE_ON; // Return the note on event. 

            } else if (current_opcode==(byte)JaiSeqEvent.WAIT_8) // Contrast to above, the opcode between these two is WAIT_U8
            {
                State.delay += Sequence.ReadByte(); // Add u8 ticks to the delay.  

                return JaiEventType.DELAY;
            } else if (current_opcode < 0x88) // We already check if it's 0x80, so anything between here will be 0x81 and 0x87
            {
                // Only the first 7 bits are going to determine which voice we're stopping. 
                State.voice = (byte)(current_opcode & 0x7F);
                return JaiEventType.NOTE_OFF;
            } else // Finally, we can fall into our CASE statement. 
            {
                switch (current_opcode)
                {
                    /* Delays and waits */
                    case (byte)JaiSeqEvent.WAIT_16: // Wait (UInt16)
                        State.delay = Sequence.ReadInt16(); // Add to the state delay
                      
                        return JaiEventType.DELAY;

                    case (byte)JaiSeqEvent.WAIT_VAR: // Wait (VLQ) see readVlq function. 
                        State.delay += Helpers.ReadVLQ(Sequence);
                        return JaiEventType.DELAY;

                    /* Logical jumps */ 

                    case (byte)JaiSeqEvent.JUMP: // Unconditional jump
                        State.jump_mode = 0; // Set jump mode to 0
                        State.jump_address = Sequence.ReadInt32(); // Absolute address. 
                        return JaiEventType.JUMP;

                    case (byte)JaiSeqEvent.JUMP_COND: // Jump based on mode
                        State.jump_mode = Sequence.ReadByte();
                        State.jump_address = (int)Helpers.ReadUInt24BE(Sequence); // pointer
                        return JaiEventType.JUMP;

                    case (byte)JaiSeqEvent.RET_COND:
                        skip(1); 
                        return JaiEventType.UNKNOWN;


                    /* Tempo Control */

                    case (byte)JaiSeqEvent.J2_SET_ARTIC: // The very same.
                    case (byte)JaiSeqEvent.TIME_BASE: // Set ticks per quarter note.
                        State.ppqn = Sequence.ReadInt16();
                        return JaiEventType.TIME_BASE;

                    case (byte)JaiSeqEvent.J2_TEMPO: // Set BPM, Same format
                    case (byte)JaiSeqEvent.TEMPO: // Set BPM
                        State.bpm = Sequence.ReadInt16();
                        return JaiEventType.TIME_BASE;

                    /* Track Control */

                    case (byte)JaiSeqEvent.OPEN_TRACK:
                        State.track_id = Sequence.ReadByte();
                        State.track_address = (int)Helpers.ReadUInt24BE(Sequence);  // Pointer to track inside of BMS file (Absolute) 
                        return JaiEventType.NEW_TRACK;
                    case (byte)JaiSeqEvent.FIN:
                        return JaiEventType.HALT;
                        

                    /* Parameter control */
                    case (byte)JaiSeqEvent.PARAM_SET_8: // Set track parameters (Usually used for instruments)
                        State.param = Sequence.ReadByte();
                        State.param_value = Sequence.ReadByte(); 
                        if (State.param==0x20) // 0x20 is bank change 
                        {
                            State.voice_bank = (byte)State.param_value; 
                            return JaiEventType.BANK_CHANGE;
                        }
                        if (State.param == 0x21) // 0x21 is program change 
                        {
                            State.voice_program = (byte)State.param_value;
                            return JaiEventType.PROG_CHANGE;
                        }
                        return JaiEventType.PARAM;

                    case (byte)JaiSeqEvent.PARAM_SET_16: // Set track parameters (Usually used for instruments)
                        State.param = Sequence.ReadByte();
                        State.param_value = Sequence.ReadInt16();
                        if (State.param == 0x20) // 0x20 is bank change 
                        {
                            State.voice_bank = (byte)State.param_value;
                            return JaiEventType.BANK_CHANGE;
                        }
                        if (State.param == 0x21) // 0x21 is program change 
                        {
                            State.voice_program = (byte)State.param_value;
                            return JaiEventType.PROG_CHANGE;
                        }
                        return JaiEventType.PARAM;
                    /* PERF Control*/
                    /* Perf structure is as follows
                     * <byte> type 
                     * <?> val
                     * (<?> dur)
                    */

                    case (byte)JaiSeqEvent.PERF_U8_NODUR:
                        State.perf = Sequence.ReadByte();
                        State.perf_value = Sequence.ReadByte();
                        State.perf_duration = 0;
                        return JaiEventType.PERF;

                    case (byte)JaiSeqEvent.PERF_U8_DUR_U8:
                        State.perf = Sequence.ReadByte();
                        State.perf_value = Sequence.ReadByte();
                        State.perf_duration = Sequence.ReadByte();
                        return JaiEventType.PERF;

                    case (byte)JaiSeqEvent.PERF_U8_DUR_U16:

                        State.perf = Sequence.ReadByte();
                        State.perf_value = Sequence.ReadByte();
                        State.perf_duration = Sequence.ReadUInt16();
                        return JaiEventType.PERF;

                    case (byte)JaiSeqEvent.PERF_S8_NODUR:
                        State.perf = Sequence.ReadByte();
                        State.perf_value = Sequence.ReadSByte();
                        State.perf_duration = 0;
                        return JaiEventType.PERF;

                    case (byte)JaiSeqEvent.PERF_S8_DUR_U8:
                        State.perf = Sequence.ReadByte();
                        State.perf_value = Sequence.ReadSByte();
                        State.perf_duration = Sequence.ReadByte();
                        return JaiEventType.PERF;

                    case (byte)JaiSeqEvent.PERF_S8_DUR_U16:
                        State.perf = Sequence.ReadByte();
                        State.perf_value = Sequence.ReadSByte();
                        State.perf_duration = Sequence.ReadUInt16();
                        return JaiEventType.PERF;

                    case (byte)JaiSeqEvent.PERF_S16_NODUR:
                        State.perf = Sequence.ReadByte();
                        State.perf_value = Sequence.ReadInt16();
                        State.perf_duration = 0;
                        return JaiEventType.PERF;

                    case (byte)JaiSeqEvent.PERF_S16_DUR_U8:
                        State.perf = Sequence.ReadByte();
                        State.perf_value = Sequence.ReadInt16();
                        State.perf_duration = Sequence.ReadByte();
                        return JaiEventType.PERF;

                    case (byte)JaiSeqEvent.PERF_S16_DUR_U16:
                        State.perf = Sequence.ReadByte();
                        State.perf_value = Sequence.ReadInt16();
                        State.perf_duration = Sequence.ReadUInt16();
                        return JaiEventType.PERF;

                    /* Unsure as of yet, but we have to keep alignment */

                    case 0xDD:
                        skip(3);
                        return JaiEventType.UNKNOWN;
                    case 0xEF:
                    case 0xF9:
                    case 0xE6:
                    case 0xE7:
                        skip(2);
                        return JaiEventType.UNKNOWN;
                    case 0xA0:
                        skip(2);
                        return JaiEventType.UNKNOWN;
                    case 0xA3:
                        skip(2);
                        return JaiEventType.UNKNOWN;
                    case 0xA5:
                        skip(2);
                        return JaiEventType.UNKNOWN;
                    case 0xA7:
                        skip(2);
                        return JaiEventType.UNKNOWN;
                    case 0xA9:
                        skip(4);
                        return JaiEventType.UNKNOWN;
                    case 0xAA:
                        skip(4);
                        return JaiEventType.UNKNOWN;
                    case 0xAD:
                        skip(3);
                        return JaiEventType.UNKNOWN;
                    case 0xAE:                        
                        return JaiEventType.UNKNOWN;
                    case 0xB1:
                        int flag = Sequence.ReadByte();
                        if (flag == 0x40) { skip(2); }
                        if (flag == 0x80) { skip(4); }
                        return JaiEventType.UNKNOWN;
                    case 0xDB:
                    case 0xDF:
                    case 0xB4:
                        skip(4);
                        return JaiEventType.UNKNOWN;
                    case 0xCB:
                        skip(2);
                        return JaiEventType.UNKNOWN;
                    case 0xCC:
                        skip(2);
                        return JaiEventType.UNKNOWN;
                    case 0xCF:
                        skip(1);
                        return JaiEventType.UNKNOWN;
                    case 0xD0:
                    case 0xD1:
                    case 0xD2:
                    case 0xD5:
                    case 0xDE:
                    case 0xDA:
                   
                        skip(1);
                        return JaiEventType.UNKNOWN;
                    case 0xF1:
                    case 0xF4:
                    case 0xE2:
                   
                    case 0xE3:
                    case 0xD6:
                        skip(1);
                        return JaiEventType.UNKNOWN;



                }


            }


            return JaiEventType.UNKNOWN_ALIGN_FAIL;
        }



    }
}
