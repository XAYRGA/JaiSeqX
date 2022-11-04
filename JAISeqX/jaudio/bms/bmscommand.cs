using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;

namespace jaudio.bms
{
    public enum BMSCommandType
    {
        INVALID = 0x00,
        NOTE_ON = 0x01,
        CMD_WAIT8 = 0x80,
        NOTE_OFF = 0x81,
        CMD_WAIT16 = 0x88,
        SETPARAM_90 = 0x90,
        SETPARAM_91 = 0x91,
        SETPARAM_92 = 0x92,
        PERF_U8_NODUR = 0x94,
        PERF_U8_DUR_U8 = 0x96,
        PERF_U8_DUR_U16 = 0x97,
        PERF_S8_NODUR = 0x98,
        PERF_S8_DUR_U8 = 0x9A,
        PERF_S8_DUR_U16 = 0x9B,
        PERF_S16_NODUR = 0x9C,
        PERF_S16_DUR_U8 = 0x9D,
        PERF_S16_DUR_U8_9E = 0x9E,
        PERF_S16_DUR_U16 = 0x9F,
        PARAM_SET_R = 0xA0,
        PARAM_ADD_R = 0xA1,
        PARAM_MUL_R = 0xA2,
        PARAM_CMP_R = 0xA3,
        PARAM_SET_8 = 0xA4,
        PARAM_ADD_8 = 0xA5,
        PARAM_MUL_8 = 0xA6,
        PARAM_CMP_8 = 0xA7,
        PARAM_LOAD_UNK = 0xA8,
        PARAM_BITWISE = 0xA9,
        PARAM_LOADTBL = 0xAA,
        PARAM_SUBTRACT = 0xAB,
        PARAM_SET_16 = 0xAC,
        PARAM_ADD_16 = 0xAD,
        PARAM_MUL_16 = 0xAE,
        PARAM_CMP_16 = 0xAF,
        OPOVERRIDE_1 = 0xB0,
        OPOVERRIDE_2 = 0xB1,
        OPOVERRIDE_4 = 0xB4,
        OPOVERRIDE_R = 0xB8,
        OPENTRACK = 0xC1,
        OPENTRACKBROS = 0xC2,
        CALL = 0xC4,
        CALLTABLE = 0xC44,
        RETURN_NOARG = 0xC5,
        RETURN = 0xC6,
        JMP = 0xC8,
        LOOP_S = 0xC9,
        LOOP_E = 0xCA,
        READPORT = 0xCB,
        WRITEPORT = 0xCC,
        CHECKPORTIMPORT = 0xCD,
        CHECKPORTEXPORT = 0xCE,
        CMD_WAITR = 0xCF,
        PARENTWRITEPORT = 0xD1,
        CHILDWRITEPORT = 0xD2,
        SETLASTNOTE = 0xD4,
        TIMERELATE = 0xD5,
        SIMPLEOSC = 0xD6,
        SIMPLEENV = 0xD7,
        SIMPLEADSR = 0xD8,
        TRANSPOSE = 0xD9,
        CLOSETRACK = 0xDA,
        OUTSWITCH = 0xDB,
        UPDATESYNC = 0xDC,
        BUSCONNECT = 0xDD,
        PAUSESTATUS = 0xDE,
        SETINTERRUPT = 0xDF,
        DISINTERRUPT = 0xE0,
        CLRI = 0xE1,
        SETI = 0xE2,
        RETI = 0xE3,
        INTTIMER = 0xE4,
        VIBDEPTH = 0xE5,
        VIBDEPTHMIDI = 0xE6,
        SYNCCPU = 0xE7,
        FLUSHALL = 0xE8,
        FLUSHRELEASE = 0xE9,
        WAIT_VLQ = 0xEA,
        PANPOWSET = 0xEB,
        IIRSET = 0xEC,
        FIRSET = 0xED,
        EXTSET = 0xEE,
        PANSWSET = 0xEF,
        OSCROUTE = 0xF0,
        IIRCUTOFF = 0xF1,
        OSCFULL = 0xF2,
        VOLUMEMODE = 0xF3,
        VIBPITCH = 0xF4,
        CHECKWAVE = 0xFA,
        PRINTF = 0xFB,
        NOP = 0xFC,
        TEMPO = 0xFD,
        TIMEBASE = 0xFE,
        FINISH = 0xFF
    }



    public abstract class bmscommand
    {
        public int OriginalAddress;
        public BMSCommandType CommandType;

        public abstract void read(BeBinaryReader read);
        public abstract void write(BeBinaryWriter write);
        public abstract string getAssemblyString(string[] data = null);

        internal string checkArgOverride(byte position, string @default, string[] data)
        {
            if (data == null || data.Length <= position || data[position] == null)
                return @default; ;
            return data[position]; ;
        }

        internal string getByteString(byte[] hx)
        {
            string x = "HEX(";
            for (int i = 0; i < hx.Length; i++)
                x += $"{hx[i]:X},";
            x += ")";
            return x;
        }

    }
}
