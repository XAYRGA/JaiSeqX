using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libJAudio;
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

        public int ticks = 0;

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
            for (int i=0; i < voices.Length; i++)
            {
                if (voices[i]!=null)
                {
                    voices[i].updateVoice();
                }
            }
            for (int i=0; i < voiceOrphans.Length; i++)
            {
                if (voiceOrphans[i] != null)
                {
                    //Console.WriteLine("UPDATE ORPHAN VOICE");
                    var voiceRes = voiceOrphans[i].updateVoice();
                    if (voiceRes == 3)
                    {
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
    
            activeVoices--;
            if (voices[id] == null)
                return;
            voices[id].stop();
      
            
            for (int i = 0; i <voiceOrphans.Length; i++)
            {
                if (voiceOrphans[i] == null)
                {
                    //Console.WriteLine("found voice orphan buffer.");
                    voiceOrphans[i] = voices[id];
                    return;
                }
            }
            voices[id] = null; // FuuF
            //*/
        }

        public void update()
        {
            updateVoices();
          
            if (delay > 0) { delay--; }
          
            if (halted) { return; }
        
            while (delay < 1) { 
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
                    case JAISeqEvent.NOTE_ON:
                        {
                            var note = trkInter.rI[0];
                            var voice = trkInter.rI[1];
                            var velocity = trkInter.rI[2];
                            var program = Registers[0x21];
                            var bank = Registers[0x20];
                            var ibnks = JaiSeqXLJA.JASystem.Banks;

                            var currentBank = ibnks[bank];
                            if (currentBank == null) { Console.WriteLine("NULL IBNK"); break; }
                            var currentInst = currentBank.Instruments[program];
                            if (currentInst==null) { Console.WriteLine("Empty inst"); break; }
                            var keyNote = currentInst.Keys[note];
                            var keyNoteVel = keyNote.Velocities[velocity];
                            JWave ouData;
                            var snd = JAISeqPlayer.loadSound(keyNoteVel.wsysid, keyNoteVel.wave, out ouData);
                            if (snd == null) { Console.WriteLine("No sound data..."); break; }

                            var newVoice = new JAIDSPVoice(ref snd);
                            newVoice.setPitch((float)Math.Pow(2, (note - ouData.key) / 12f) * currentInst.Pitch * keyNoteVel.Pitch);
                            if (currentInst.oscillatorCount > 0)
                            {
                                newVoice.setOcillator(currentInst.oscillators[0]);
                            }
                            newVoice.play();
                            addVoice(newVoice, (byte)voice);



                            break;
                        }
                    case JAISeqEvent.NOTE_OFF:
                        stopVoice((byte)trkInter.rI[0]);
                        break;
                }
     
            }
          
        }
    }
}
