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
        JAIDSPVoice[] voiceOrphans;

        public int activeVoices;

        public JAISeqTrack parent;

        public static bool muted;
        public static bool halted;

        public JAISeqTrack(ref byte[] SeqFile, int address)
        {
            bmsData = SeqFile;
            offsetAddr = address;
            trkInter = new JAISeqInterpreter(ref SeqFile, address);
            voices = new JAIDSPVoice[0xA]; // Even though we only support 7 voices, I can tell that some will linger whenever we stop them.            
            voiceOrphans = new JAIDSPVoice[0xFF];
        }

        public void destroy()
        {

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
                        voices[i].Dispose();
                        voices[i] = null;
                    }
                }
            }
            for (int i=0; i < voiceOrphans.Length; i++)
            {
                if (voiceOrphans[i] != null)
                {
                    var voiceRes = voiceOrphans[i].updateVoice();
                    if (voiceRes == 3)
                    {
                        voiceOrphans[i].Dispose();
                        voiceOrphans[i] = null;
                    }
                }
            }
        }

        private void addVoice(JAIDSPVoice voice, byte id)
        {
            activeVoices++;
            voices[id] = voice;
           // Console.WriteLine("VOICE BUFFER FULL: Trk{0} BA: 0x{1:X} PC: 0x{2:X}\n\nPREPARE FOR THE LEAKENING.", trackNumber, offsetAddr, trkInter.pc);
        }
        private void stopVoice(byte id)
        {
            if (voices[id] == null)
                return;
            voices[id].stop();
            for (int i = 0; i <voiceOrphans.Length; i++)
            {
                if (voiceOrphans[i] == null)
                {
                    voiceOrphans[i] = voices[id];
                    return;
                }
            }
        }

        public void update()
        {
            updateVoices();
            if (delay > 0) { delay--; return; }
            if (halted) { return; }
            while (delay <= 0) { 
               var opcode = trkInter.loadNextOp(); // load next operation
                //Console.WriteLine(opcode);
                switch (opcode)
                {
                    case JAISeqEvent.WAIT_8:
                    case JAISeqEvent.WAIT_16:
                    case JAISeqEvent.WAIT_VAR:
                        delay += trkInter.rI[0];
                        break;
                    case JAISeqEvent.OPEN_TRACK:
                        {
                            var newTrk = new JAISeqTrack(ref bmsData, trkInter.rI[1]);
                            newTrk.trackNumber = trkInter.rI[0];
                            JAISeqPlayer.addTrack(newTrk.trackNumber,newTrk);
                            break;                               
                        }
                    case JAISeqEvent.PARAM_SET_16:
                    case JAISeqEvent.PARAM_SET_8:
                        {
                            Registers[(byte)trkInter.rI[0]] = (short)trkInter.rI[1];
                            break;
                        }
                    case JAISeqEvent.FIN:
                        halted = true;
                        break;
                   
                    case JAISeqEvent.J2_SET_ARTIC:
                    case JAISeqEvent.TIME_BASE:
                        JAISeqPlayer.ppqn = trkInter.rI[0];
                        JAISeqPlayer.recalculateTimebase();
                        break;
                    case JAISeqEvent.J2_TEMPO:
                    case JAISeqEvent.TEMPO:
                        JAISeqPlayer.bpm = trkInter.rI[0];
                        JAISeqPlayer.recalculateTimebase();
                        break;
                }
     
            }
        }
    }
}
