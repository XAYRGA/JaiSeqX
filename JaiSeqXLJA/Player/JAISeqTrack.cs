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

        public bool muted;
        public bool halted;

        float perfTarget = 0;
        int perfTicks = 0;


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


        private bool checkCondition(byte cond)
        {
            var conditionValue = Registers[3];
            // Explanation:
            // When a compare function is executed. the registers are subtracted. 
            // The subtracted result is stored in R3. 
            // This means:

            switch (cond)
            {
                case 0: // We were probably given the wrong commmand
                    return true;  // oops, all boolean
                case 1: // Equal, if r1 - r2 == 0, then they obviously both had the same value
                    if (conditionValue == 0) { return true; }
                    return false;
                case 2: // Not Equal, If r1 - r2 doesn't equal 0, they were not the same value.
                    if (conditionValue != 0) { return true; }
                    return false;
                case 3: // One good question.
                    if (conditionValue == 1) { return true; }
                    return false;
                case 4: // Less Than if r1 - r2 is more than zero, this means r1 was less than r2
                    if (conditionValue > 0) { return true; }
                    return false;
                case 5: // Greater than , if r1 - r2 is less than 0, that means r1 was bigger than r2
                    if (conditionValue < 0) { return true; }
                    return false;
            }
            return false;
        }

        private void crash()
        {
            Console.WriteLine("[!] Track {0} crashed at {1:X}", trackNumber, trkInter.pc);
            halted = true;
            var finstack = new Queue<JAISeqExecutionFrame>(trkInter.history.Reverse<JAISeqExecutionFrame>()); // Reverse history
            try
            {
                var finaddr = finstack.Dequeue();
                var depth = 0;
                var b = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("({2:X}) 0x{0:X}: 0x{1:X}", finaddr.address, finaddr.opcode, depth);
                Console.ForegroundColor = b;
                while (true)
                {
                    depth++;
                    if (finstack.Count == 0) { break; }
                    finaddr = finstack.Dequeue();
                    Console.WriteLine("\t({2:X}) 0x{0:X}: 0x{1:X}", finaddr.address, finaddr.opcode, depth);
                }

            }
            catch { }
            
        }


        public void update()
        {
            updateVoices();
          
            if (delay > 0) { delay--; }
          
            if (halted) { return; }
        
            while (delay < 1) { 
               var opcode = trkInter.loadNextOp(); // load next operation\
               // Console.WriteLine(opcode);
   
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
                    case JAISeqEvent.JUMP_CONDITIONAL:
                        if (checkCondition((byte)trkInter.rI[0]))
                        {
                            trkInter.jump(trkInter.rI[1]);
                            Console.WriteLine("Trk{0} jumps to {1}", trackNumber, trkInter.rI[1]);
                        }
                        else
                            Console.WriteLine("Trk {0} cond check jump fail.", trackNumber);
                        break;

                    case JAISeqEvent.JUMP:
                        trkInter.jump(trkInter.rI[0]);
                        break;
                    case JAISeqEvent.FIN:
                        halted = true;
                        break;
                   
                    case JAISeqEvent.J2_SET_ARTIC:
                    case JAISeqEvent.TIME_BASE:
                        JAISeqPlayer.ppqn = trkInter.rI[0];
                        JAISeqPlayer.recalculateTimebase();
                        Console.WriteLine("RECALC TIMEBASE.");
                        break;
                    case JAISeqEvent.J2_TEMPO:
                    case JAISeqEvent.TEMPO:
                        Console.WriteLine("RECALC TIMEBASE BPM");
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
                            if (snd == null) { Console.WriteLine("*screams in null wave buffer*"); break; }

                            var newVoice = new JAIDSPVoice(ref snd);
                            newVoice.setPitchMatrix(0,(float)Math.Pow(2, (note - ouData.key) / 12f) * currentInst.Pitch * keyNoteVel.Pitch);
                            newVoice.setVolumeMatrix(0, (float)((velocity / 127f * currentInst.Volume * keyNoteVel.Volume) * 0.5f * 0.6f) );
                            if ((float)((velocity / 127f * currentInst.Volume * keyNoteVel.Volume) * 0.5f * 0.6f) > 1)
                            {
                                Console.WriteLine("[!]");
                            }

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
                    case JAISeqEvent.UNKNOWN:
       
                        break;

                }
     
            }
          
        }
    }
}
