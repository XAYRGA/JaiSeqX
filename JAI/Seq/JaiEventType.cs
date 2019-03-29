using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JaiSeqX.JAI.Seq
{
    public enum JaiEventType
    {
        NOTE_ON = 0x00,
        NOTE_OFF = 0x01,
        DELAY = 0x02,
        TIME_BASE = 0x03,
        NEW_TRACK = 0x04,
        BANK_CHANGE = 0x05,
        PROG_CHANGE = 0x06,
        JUMP = 0x07,
        PARAM = 0x08,
        PERF = 0x09,
        CALL = 0x10,
        RET = 0x11,

        HALT = 0xFC,
        PAUSE = 0xFD,

        UNKNOWN_ALIGN_FAIL = 0xFE,
        UNKNOWN = 0xFF,


    }

}
