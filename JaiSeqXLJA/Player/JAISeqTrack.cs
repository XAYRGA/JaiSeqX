using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libJAudio.Sequence;
using libJAudio.Sequence.Inter;
using JaiSeqXLJA.DSP;

namespace JaiSeqXLJA.Player
{
    public class JAISeqTrack
    {
        JAISeqInterpreter trkInter;
        JAITrackRegisterMap Registers = new JAITrackRegisterMap();
        
        byte[] bmsData;
        int offsetAddr;

        public Stack<int> CallStack = new Stack<int>(32);
        public int[] Ports = new int[32];

        public int trackNumber;
        public int delay;

        JAIDSPVoice[] voices;
        public int activeVoices;

        public JAISeqTrack(ref byte[] SeqFile, int address)
        {
            bmsData = SeqFile;
            offsetAddr = address;
            trkInter = new JAISeqInterpreter(ref SeqFile, address);
            voices = new JAIDSPVoice[0xFF]; // Even though we only support 7 voices, I can tell that some will linger whenever we stop them.            
        }

        private void updateVoices()
        {
            for (int i = 0; i < voices.Length; i++)
            {
                if (voices[i]!=null)
                {
                    var voiceRes = voices[i].updateVoice();
                    if (voiceRes==3)
                    {
                        voices[i] = null;
                    }
                }
            }
        }

        private void addVoice(JAIDSPVoice voice)
        {
            for (int i = 0; i < voices.Length; i++)
            {
                if (voices[i]==null)
                {
                    voices[i] = voice;
                    return;
                }
            }
            Console.WriteLine("VOICE BUFFER FULL: Trk{0} BA: 0x{1:X} PC: 0x{2:X}\n\nPREPARE FOR THE LEAKENING.", trackNumber, offsetAddr, trkInter.pc);
        }

        public void update()
        {
            updateVoices();
            if (delay > 0) { delay--; return;}
            var opcode = trkInter.loadNextOp(); // load next operation


        }
    }
}
