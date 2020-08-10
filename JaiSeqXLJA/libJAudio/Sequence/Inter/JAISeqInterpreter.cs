using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;
using System.IO;

namespace libJAudio.Sequence.Inter            
{
    public enum JAISeqInterpreterVersion // Futile attempt to save my codebase.
    {
        JA1 = 0,
        JA2 = 1
    }

    public partial class JAISeqInterpreter
    {
       
        byte[] SeqData; // Full .BMS  file.  
        BeBinaryReader Sequence; // Reader for SeqData
        private int baseAddress; // The address at which this subroutine starts in the file.
        public byte last_opcode; // The last opcode that was executed.
        public Stack<int> AddrStack; // JAISeq return stack, depth of 8, used for CALL and RETURN commands.
        public Queue<JAISeqExecutionFrame> history; // Execution history.  
        public int[] rI; // Internal Integer registers  -- for interfacing with sequence. 
        public float[] rF; // Internal Float registers -- for interfacing with sequence.
        public JAISeqInterpreterVersion InterpreterVersion;

        public int pc // Current Program Counter
        {
            get {return (int)Sequence.BaseStream.Position; }
            set { Sequence.BaseStream.Position = value; }
        }
        public int pcl;
        public JAISeqInterpreter(ref byte[] BMSData,int BaseAddr, JAISeqInterpreterVersion ver)
        {
            SeqData = BMSData; // 
            AddrStack = new Stack<int>(8); // JaiSeq has a stack depth of 8
            history = new Queue<JAISeqExecutionFrame>(32); // Ill keep an opcode depth of 32     
            Sequence = new BeBinaryReader(new MemoryStream(BMSData)); // Make a reader for this. 
            Sequence.BaseStream.Position = BaseAddr; // Set its position to the base address. 
            baseAddress = BaseAddr; // store the base address
            rI = new int[8]; 
            rF = new float[8];
           // Console.WriteLine($"Initialized with intver {ver}");
            InterpreterVersion = ver;
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
            Sequence.BaseStream.Position = pos;
        }
        public JAISeqEvent loadNextOp()
        {
            if (history.Count == 32)  // Opstack is full
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
                    default:
                        {
                            JAISeqEvent ret;
                            ret = ProcessFlowOps(current_opcode); // Check for flow ops, like wait commands
                            if (ret != JAISeqEvent.UNKNOWN) 
                                return ret;
                            ret = ProcessPerfOps(current_opcode); // Second most common are PERF ops. Such as pitch bend / etc
                            if (ret != JAISeqEvent.UNKNOWN)
                                return ret;
                            ret = ProcessParamOps(current_opcode); // third most common would be params, like bank change
                            if (ret != JAISeqEvent.UNKNOWN)
                                return ret;                     
                            ret = ProcessArithmeticOps(current_opcode); // fourth most common is arithmetic
                            if (ret != JAISeqEvent.UNKNOWN)
                                return ret;                    
                            break; // We didn't find anything, and this is our default case -- drop out. 
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
                            Console.WriteLine(v);
                            var l = Sequence.ReadByte();
                            if (l != 0)
                                Sequence.BaseStream.Position = Sequence.BaseStream.Position - 1;
                            return JAISeqEvent.UNKNOWN;
                        }
                    case (byte)JAISeqEvent.SYNC_CPU:
                       skip(2);
                        // Console.WriteLine(Sequence.ReadByte());
                        //Console.WriteLine(Sequence.ReadByte());
                        return JAISeqEvent.SYNC_CPU;
                    /* 3 byte unknowns */
                    case 0xDD:
                    case (byte)JAISeqEvent.FIRSTSET:
                    case (byte)JAISeqEvent.LASTSET:
                    case (byte)JAISeqEvent.TRANSPOSE:
                    case (byte)JAISeqEvent.CLOSE_TRACK:
                        skip(3);
                        return JAISeqEvent.UNKNOWN;
                    /* 4 byte unknowns */
                    case (byte)JAISeqEvent.OUTSWITCH:
                    case (byte)JAISeqEvent.INTERRUPT:
                    case (byte)JAISeqEvent.BITWISE:
                    case (int)JAISeqEvent.LOADTBL:
                    //case (byte)JAISeqEvent.CLOSE_TRACK:
                        skip(4);
                        return JAISeqEvent.UNKNOWN;
                    /* special case unknowns? */
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
                    /* 2 Byte Unknowns */
                    case (byte)JAISeqEvent.PANSWEEPSET:
                    case 0xF9:
                    case (byte)JAISeqEvent.CONNECT_CLOSE:
                    case 0xBE: // Completely unknown
                    case (byte)JAISeqEvent.WRITE_CHILD_PORT:
                    case (byte)JAISeqEvent.CONNECT_NAME:
                   
                        skip(2);
                        return JAISeqEvent.UNKNOWN;
                    /* One byte unknowns */

                    case 0xD5: // TIMERELATE
            
                    case 0xDE: // don't know either.
                    case (byte)JAISeqEvent.IRCCUTOFF:
                    case 0xF4:
                    case (byte)JAISeqEvent.SIMPLE_OSC:
                        skip(1);
                        //Console.WriteLine(Sequence.ReadByte());
                        return JAISeqEvent.UNKNOWN;
                    case 0xBC: // nobody knows what the actual fuck this is. 
                        return JAISeqEvent.UNKNOWN;
                }
            }
            return JAISeqEvent.MISS; // ABSOLUTE FUCKING DEATH. 
        }
    }
}
