using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libJAudio.Sequence
{

    /*    
     _______     ________       _       ______     ____    ____  ________  
    |_   __ \   |_   __  |     / \     |_   _ `.  |_   \  /   _||_   __  | 
      | |__) |    | |_ \_|    / _ \      | | `. \   |   \/   |    | |_ \_| 
      |  __ /     |  _| _    / ___ \     | |  | |   | |\  /| |    |  _| _  
     _| |  \ \_  _| |__/ | _/ /   \ \_  _| |_.' /  _| |_\/_| |_  _| |__/ | 
    |____| |___||________||____| |____||______.'  |_____||_____||________|                                                                   
        THESE OPCODES ARE ABSOLUTELY NOT ACCURATE TO THE ACTUAL JAISEQ SYSTEM.
        They are an attempt to match only, nothing more. 
        They don't match because JAISeq1 and JAISeq2 have colliding opcodes.

        For example, 0xFD in JAISeq1 is TIME_BASE, 
        but 0xFD in JAISeq2 is PRINTF,
        Obviously, a single case statement, and an enumerator can't 

        For actual opcode ID's, see seqdoc.txt
        Or just look at the goddamn source files. they're commented.
    */
      

    public enum JAISeqEvent
    {
        UNKNOWN = 0x00,
        NOTE_ON = 0x01,
        NOTE_OFF = 0x02,
        MISS = 0x03,

        /* wait with u8 arg */
        WAIT_8 = 0x80, // WAIT <byte wait time>
        /* wait with u16 arg */
        WAIT_16 = 0x88, // WAIT <short wait time> 
        /* Varlen */
        WAIT_VAR = 0xF0, // WAIT <int24 wait time>, i think its actually a 24 bit wait? otherwise, VLQ.
        /* Wait register length */ 
        WAIT_REGISTER = 0xCF, // WAIT <byte register> 

        OPEN_TRACK = 0xC1, // OPENTRACK <byte track id> <int24 address>
        OPEN_TRACK_BROS = 0xC2, // Never used?
        
        CALL = 0xC3, // <int32 address>
        CALL_CONDITIONAL = 0xC4,  // CALL <byte condition> <int24 address>
        RETURN = 0xC5, // RETURN
        RETURN_CONDITIONAL = 0xC6, // RETURN <byte condition> 
        JUMP = 0xC7, // JUMP <int32 address>
        JUMP_CONDITIONAL = 0xC8, // JUMP <byte condition> <int24 address>
        LOOPS = 0xC9, // Loop Start 
        LOOPE = 0xCA, // Loop End / Do Loop 
        READPORT = 0xCB, // <byte flags> <byte destination register> 
        WRITEPORT = 0xCC, // <byte port> <byte value>
        CHECK_PORT_IMPORT = 0xCD, 
        CHECK_PORT_EXPORT = 0xCE, 
        // Wait register (0xCF), never used. 



        /* writeRegParam */
        PARAM_SET_R = 0xA0, // byte source register, byte destination register
            ADDR = 0xA1, // <byte destination_reg> <byte source_reg>
            MULR = 0xA2, // <byte destination_reg> <byte source_reg>
            CMPR = 0xA3,// <byte destination_reg> <byte source_reg>
        PARAM_SET_8 = 0xA4, // <byte register> <byte value>
            ADD8 = 0xA5, // <byte destination_reg> <byte value>
            MUL8 = 0xA6, // <byte destination_reg> <byte value>
            CMP8 = 0xA7, // <byte destination_reg> <byte value>
        LOADTBL = 0xAA,
        SUBTRACT = 0xAB,
        PARAM_SET_16 = 0xAC, // <byte register> <short value>
            ADD16 = 0xAD, // <byte destination_reg> <short value>
            MUL16 = 0xAE, // <byte destination_reg> <short value>
            CMP16 = 0xAF, // <byte destination_reg> <short value>
        //**************//


        /* perf / lerp */
        PERF_U8_NODUR = 0x94, // <byte param> <byte value>
        PERF_U8_DUR_U8 = 0x96, // <byte param> <byte value> <byte duration_ticks>
        PERF_U8_DUR_U16 = 0x97,// <byte param> <byte value> <short duration_ticks>
        PERF_S8_NODUR = 0x98, // <byte param> <sbyte value> 
        PERF_S8_DUR_U8 = 0x9A, // <byte param> <sbyte value> <byte duration_ticks>
        PERF_S8_DUR_U16 = 0x9B, // <byte param> <sbyte value> <short duration_ticks>
        PERF_S16_NODUR = 0x9C, // <byte param> <short value>
        PERF_S16_DUR_U8 = 0x9E, // <byte param> <short value> <byte duration_ticks>
        PERF_S16_DUR_U16 = 0x9F, // <byte param> <short value> <short duration_ticks>

        //************//

        BITWISE = 0xA9,
        CONNECT_NAME = 0xD0,
        CONNECT_CLOSE = 0xE6,
        WRITE_PARENT_PORT = 0xD1, // <byte destination_port> <byte value>  // have no clue
        WRITE_CHILD_PORT = 0xD2,  // <byte destination_port> <byte value>  // have no clue. 
        PAUSE_STATUS = 0xD3, // <byte status> 
        SET_LAST_NOTE = 0xD4, // <byte note> 
        TIMERELATE = 0xD5,
        SIMPLE_OSC = 0xD6,
        SIMPLE_ENV = 0xD7,
        SIMPLE_ADSR = 0xD8,
        TRANSPOSE = 0xD9,
        CLOSE_TRACK = 0xDA, // <byte track-id> 
        BUSCONNECT = 0xDD,
        INTERRUPT = 0xDF,
       
        IRCCUTOFF = 0xF1,
        OUTSWITCH = 0xDB,
        FIRSTSET = 0xED,
        LASTSET = 0xEE,

        INTERRUPT_TIMER = 0xE4, // 
        SYNC_CPU = 0xE7, // <short max_wait> 
        PANSWEEPSET = 0xEF, // <byte speed>?
        OSCILLATORFULL = 0xF2, 
        VOLUME_MODE = 0xF3, // <byte mode>
        PRINTF = 0xFB, // READ UNTIL 0x00, advance one byte.
        NOP = 0xFC, // NO ARGS
        TIME_BASE = 0xFD, // Short tempo
        TEMPO = 0xFE, // <short timebase>
        FIN = 0xFF, // NO ARGS

        // Thanks, Jasper!
        /* "Improved" JaiSeq from TP / SMG / SMG2 seems to use this instead */
        J2_SET_PARAM_8 = 0x02B8, // <byte register> <byte value>
        J2_SET_PARAM_16 = 0x02B9, // <byte register> <short value>
        /* Set "articulation"? Used for setting timebase. */
        J2_SET_ARTIC = 0x02D8, // <short timebase>
        J2_TEMPO = 0x02E0, // <short tempo>
        J2_SET_BANK = 0x02E2, // <byte bank>
        J2_SET_PROG = 0x02E3, // <byte program>
        J2_PRINTF = 0x02F9, // mActual opcode is 0xFD
        J2_UNK0 = 0x02D1

    }

}
