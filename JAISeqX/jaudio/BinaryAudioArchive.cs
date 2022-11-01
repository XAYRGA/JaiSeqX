using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jaudio
{
    internal partial class BinaryAudioArchive : AudioArchive
    {
        public const int BAA_Header = 0x41415F3C;
        public const int BAA_BAAC = 0x62616163; // BINARY AUDIO ARCHIVE CUSTOM
        public const int BAA_BFCA = 0x62666361;
        public const int BAA_BMS = 0x626D7320; // BINARY MUSIC SEQUENCE
        public const int BAA_BNK = 0x626E6B20; // INSTRUMENT BANK
        public const int BAA_BSC = 0x62736320; // BINARY SEQUENCE COLLECTION
        public const int BAA_BSFT = 0x62736674; // BINARY STREAM FILE TABLE
        public const int BAA_BST = 0x62737420; // BINARY SOUND TABLE
        public const int BAA_BSTN = 0x6273746E; // BINARY SOUND TABLE NAME
        public const int BAA_WS = 0x77732020; // WAVE SYSTEM
        public const int BAA_Footer = 0x3E5F4141;
    }
}
