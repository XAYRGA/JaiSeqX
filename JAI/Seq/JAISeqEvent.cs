using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JaiSeqX.JAI.Seq
{

    public enum JAISeqEvent2
    {

        /* wait with u8 arg */
        WAIT_8 = 0x80, // WAIT <byte wait time>
        /* wait with u16 arg */
        WAIT_16 = 0x88, // WAIT <short wait time> 
        /* Varlen */
        WAIT_VAR = 0xF0, // WAIT <int24 wait time>, i think its actually a 24 bit wait? otherwise, VLQ.
        /* Wait register length */ 
        WAIT_REGISTER = 0xCF, // WAIT <byte register> 

        OPEN_TRACK = 0xC1, // OPENTRACK <byte track id> <int24 address>
        OPEN_TRACK_BROS = 0xC2, 
        
        CALL = 0xC3, // <int32 address>
        CALL_CONDITIONAL = 0xC4,  // CALL <byte condition> <int24 address>
        RETURN = 0xC5, // RETURN
        RETURN_CONDITIONAL = 0xC6, // RETURN <byte condition> 
        JUMP = 0xC7, // JUMP <int32 address>
        JUMP_CONDITIONAL = 0xC8, // JUMP <byte condition> <int24 address>
        LOOPS = 0xC9, // Loops?
        LOOPE = 0xCA, // LoopE
        READPORT = 0xCB, // <byte flags> <byte destination register> 
        PORTWRITE = 0xCC,
        CHECK_PORT_IMPORT = 0xCD,
        CHECK_PORT_EXPORT = 0xCE, 
        // Wait register (0xCF), never used. 

        

        /* writeRegParam */
        PARAM_SET = 0xA0,
            ADDR = 0xA1,
            MULR = 0xA2,
            CMPR = 0xA3,
        PARAM_SET_8 = 0xA4,
            ADD8 = 0xA5,
            MUL8 = 0xA6,
            CMP8 = 0xA7,
        LOADTBL = 0xAA,
        SUBTRACT = 0xAB,
        PARAM_SET_16 = 0xAC,
            ADD16 = 0xAD,
            MUL16 = 0xAE,
            CMP16 = 0xAF,
        LOAD_TABLE = 0xAA,
        BITWISE = 0xA9,


        CONNECT_NAME = 0xD0,
        WRITE_PARENT_PORT = 0xD1,
        WRITE_CHILD_PORT = 0xD2, 

        SIMPLE_ADSR = 0xD8,

        BUSCONNECT = 0xDD,
        INTERRUPT = 0xDF,
        INTERRUPT_TIMER = 0xE4,
        SYNC_CPU = 0xE7,
        PANSWSET = 0xEF,
        OSCILLATORFULL = 0xF2,
        PRINTF = 0xFB,
        NOP = 0xFC,
        TEMPO = 0xFD,
        TIME_BASE = 0xFE,
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

    public enum JAISeqEvent
    {


        /* wait with variable-length arg */
      

        /* perf / lerp */
        PERF_U8_NODUR = 0x94,
        PERF_U8_DUR_U8 = 0x96,
        PERF_U8_DUR_U16 = 0x97,
        PERF_S8_NODUR = 0x98,
        PERF_S8_DUR_U8 = 0x9A,
        PERF_S8_DUR_U16 = 0x9B,
        PERF_S16_NODUR = 0x9C,
        PERF_S16_DUR_U8 = 0x9E,
        PERF_S16_DUR_U16 = 0x9F,





        OPEN_TRACK = 0xC1,
        OPEN_TRACK_BROS = 0xC2,
        CALL = 0xC3,
        CALL_COND = 0xC4,
        RET = 0xC5,
        RET_COND = 0xC6,
        JUMP = 0xC7,
        JUMP_COND = 0xC8,
        LOOP_S = 0xC9,
        READPORT = 0xCA,
        PORTWRITE = 0xCB,
        CHECK_PORT_IMPORT = 0xCC,
        CHECK_PORT_EXPORT = 0xCD,
        WAIT_REGISTER = 0xCE,
        CONNECT_NAME = 0xCF,
        PARENT_WRITE_PORT = 0xD0,
        CHILD_WRITE_PORT = 0xD1,
        CONNECT_CLOSE = 0xE6,
        CONNECT_OPEN = 0xE5, 
        INT_TIMER = 0xE4,




        NAMEBUS = 0xD0,
        ADSR = 0xD8,
        BUSCONNECT = 0xDD,
        INTERRUPT = 0xDF,
        INTERRUPT_TIMER = 0xE4,
        SYNC_CPU = 0xE7,
        PANSWSET = 0xEF,
        OSCILLATORFULL = 0xF2,
        PRINTF = 0xFB,
        NOP = 0xFC,
        TEMPO = 0xFD,
        TIME_BASE = 0xFE,
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

}
