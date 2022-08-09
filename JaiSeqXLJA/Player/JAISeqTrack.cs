using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libJAudio;
using libJAudio.Sequence;
using libJAudio.Sequence.Inter;
using JaiSeqXLJA.DSP;
using System.Security.Cryptography;

namespace JaiSeqXLJA.Player
{
    public class JAISeqTrack
    {
        JAISeqInterpreter trkInter;
        public JAITrackRegisterMap Registers = new JAITrackRegisterMap();
        
        byte[] bmsData;
        int offsetAddr;
        JAISeqInterpreterVersion interVer;

        public Stack<int> CallStack = new Stack<int>(32);
        public int[] Ports = new int[32];
        public int trackNumber;
        public int delay;
        public int lastDelay;
        float volume = 1;

        public int looppos = 0;
        

        JAIDSPVoice[] voices;
        JAIDSPVoice[] voiceOrphans;
        private int trackArticulation = 1;
        public int activeVoices;
        public string lastOpcode;
        

        public JAISeqTrack parent;

        public bool muted;
        public bool halted;
        public bool crashed; 

        private static byte[] bendCoefLUT;
        bool bending = false;
        int bendticks = 0;
        int bendTargetTicks = 0;
        int bendTarget = 0;
        float bendFinalValue = 1f;

        public int ticks = 0;

        public JAISeqTrack(ref byte[] SeqFile, int address, JAISeqInterpreterVersion seqVersion)
        {
            bmsData = SeqFile;
            offsetAddr = address;
            trkInter = new JAISeqInterpreter(ref SeqFile, address, seqVersion);
            voices = new JAIDSPVoice[0xA]; // Even though we only support 7 voices, I can tell that some will linger whenever we stop them.            
            voiceOrphans = new JAIDSPVoice[0xFF];
            interVer = seqVersion;



            bendCoefLUT = new byte[100];
            bendCoefLUT[12] = 2;
            bendCoefLUT[8] = 2;
            bendCoefLUT[2] = 12;
            bendCoefLUT[0] = 2;
           

        }

        public  int pc
        {
            get
            {
                return trkInter.pc;
            }
        }

        public void destroy()
        {
            for (int i = 0; i < voices.Length; i++)
            {
                if (voices[i] != null)
                {
                    voices[i].forceStop() ;
                }

            }
            for (int i = 0; i < voiceOrphans.Length; i++)
            {
                if (voiceOrphans[i] != null)
                {
                    //Console.WriteLine("UPDATE ORPHAN VOICE");
                   voiceOrphans[i].forceStop();             
                }
            }
        }

        public void purgeVoices()
        {
            for (int i = 0; i < voices.Length; i++)
            {
                if (voices[i] != null)
                    voices[i].forceStop();
            }
        }

        public float Lerp(float firstFloat, float secondFloat, float by)
        {
            return firstFloat * (1 - by) + secondFloat * by;
        }

        public void updateTrackVolume(float volume)
        {

            for (int i = 0; i < voices.Length; i++)
            {
                if (voices[i] != null)
                {  
                    voices[i].setVolumeMatrix(2,volume);
                }

            }
        }
        private void updateVoices()
        {
            for (int i=0; i < voices.Length; i++)
            {               
                if (voices[i]!=null)
                {
                    if (bending==true)
                    {
                        bendticks++;
                        var bendCoef = bendCoefLUT[Registers[7]];
                        var bend = (float)Math.Pow(2, (( (float)bendTarget)) / (4096f * bendCoef));
                        var cnt = voices[i].getPitchMatrix(1);
                        voices[i].setPitchMatrix(1, Lerp(cnt,bend,1f));
                        bendFinalValue = bend;
                        if (bendticks > bendTargetTicks)
                        {
                            bending = false;
                        }
                        
                    }
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
            stopVoice(id);
            voices[id] = voice;
            if (Registers[7]==2)
            {
                voices[id].setPitchMatrix(1, (float)bendTarget / (4096f * 12) );
            }
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
                    //Console.WriteLine("found voice orphan buffer.");
                    voiceOrphans[i] = voices[id];
                    break;
                }
            }
            voices[id] = null; // FuuF
            activeVoices--;
            //*/
        }


        private bool checkCondition(byte cond)
        {
            var conditionValue = Registers[0];
            // Explanation:
            // When a compare function is executed. the registers are subtracted. 
            // The subtracted result is stored in R3. 
            // This means:

            //if (Console.KeyAvailable) {
            /*
                var w = Console.ReadKey();

                //JAISeqPlayer.cycleTrackMuted((int)w.Key - 64);

                if (w.Key != ConsoleKey.A)
                {
                    return true;
                } else { return false; }
                */
            //}
               // */
            

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
            crashed = true;
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
            while (delay < 1 && !halted) {
                Registers[3] = 2;
                var opcode = JAISeqEvent.UNKNOWN;
          
                try
                {
                    opcode = trkInter.loadNextOp(); // load next operation\
                   
                } catch (Exception E)
                {
                    Console.WriteLine("TRACK {0} CATASTROPHIC CRASH", trackNumber);
                    crash();
                    halted = true;
                    Console.WriteLine(E.ToString());
                    return;
                }

                if (opcode != JAISeqEvent.WAIT_8 && opcode != JAISeqEvent.WAIT_16 && opcode != JAISeqEvent.WAIT_VAR) //&& opcode!=JAISeqEvent.NOTE_OFF && opcode!=JAISeqEvent.NOTE_ON) 
                {
                    lastOpcode = opcode.ToString();
                   // Console.WriteLine($"(0x{pc:X} [{trackNumber}]:{opcode.ToString()} (0x{(int)opcode:X})");

                }

                //Console.ReadLine();
                if (trackNumber==-1)
                {
                    //muted = true;
                }

                if (trackNumber==0)
                {
                   // muted = true;
                }
   
                switch (opcode)
                {
                    case JAISeqEvent.READPORT:
                        Registers[0] = 3;
                        break;
                    case JAISeqEvent.PERF_S8_NODUR:
                    case JAISeqEvent.PERF_S8_DUR_U8:
                    case JAISeqEvent.PERF_S8_DUR_U16:
                    case JAISeqEvent.PERF_U8_NODUR:
                    case JAISeqEvent.PERF_U8_DUR_U8:
                    case JAISeqEvent.PERF_U8_DUR_U16:
                    case JAISeqEvent.PERF_S16_NODUR:
                    case JAISeqEvent.PERF_S16_DUR_U8:
                    case JAISeqEvent.PERF_S16_DUR_U16:
                        /*             rI[0] = perf;
                        rI[1] = (value > 0x7F) ? value - 0xFF : value;
                        rI[2] = 0;
                        rF[0] = ((float)(rI[1]) / 0x7F);
                        */
                        {
                            if (trkInter.rI[0]==1)
                            {
                                bending = true;
                                bendTargetTicks = trkInter.rI[2];
                                bendTarget = trkInter.rI[1];
                                bendticks = 0;
                            } else if (trkInter.rI[0]==0)
                            {
                                volume = trkInter.rF[0];
                                //Console.WriteLine("Trk{0} change volume {1}", trackNumber, trkInter.rF[0]);
                                updateTrackVolume(volume); 
                            }

                            break;
                        }

                    case JAISeqEvent.LOOPS:
                        looppos = trkInter.pc;
                        break;
                    case JAISeqEvent.LOOPE:
                        trkInter.jump(looppos);
                        break;
                    case JAISeqEvent.WAIT_8:
                    case JAISeqEvent.WAIT_16:
                    case JAISeqEvent.WAIT_VAR:
                        delay += trkInter.rI[0];
                        lastDelay = delay;
                        break;
                    case JAISeqEvent.OPEN_TRACK:
                        {
                            var newTrk = new JAISeqTrack(ref bmsData, trkInter.rI[1],interVer);
                            newTrk.trackNumber = trkInter.rI[0];
                            JAISeqPlayer.addTrack(newTrk.trackNumber,newTrk);
                            break;                               
                        }
                    case JAISeqEvent.J2_SET_PARAM_8:
                    case JAISeqEvent.J2_SET_PARAM_16:
                        {
                            //Console.WriteLine("{0} {1}", trkInter.rI[0], trkInter.rI[1]);
                            Registers[(byte)trkInter.rI[0]] = (short)trkInter.rI[1];
                            if ((byte)trkInter.rI[0]==1)
                            {
                                bending = true;
                                bendTargetTicks = 1;
                                bendTarget = trkInter.rI[1];
                                bendticks = 0;
                            }
                            break;
                        }
                    case JAISeqEvent.PARAM_SET_16:
                    case JAISeqEvent.PARAM_SET_8:
                        {
                            Registers[(byte)trkInter.rI[0]] = (short)trkInter.rI[1];

                            break;
                        }
                    case JAISeqEvent.JUMP_CONDITIONAL:
                        if (checkCondition((byte)(trkInter.rI[0] & 15)))
                        {
                            trkInter.jump(trkInter.rI[1]);
                            Console.WriteLine("T({0}) jmp C-> {1:X}", trackNumber, trkInter.rI[1]);
                        }
                        else
                            Console.WriteLine("skip T({0}) jmp C-!> : {1} {2:X} (condition fail)", trackNumber,trkInter.rI[0] & 15,trkInter.rI[1]);
                        break;
                    case JAISeqEvent.CALL:
         
                        CallStack.Push(trkInter.pc);
                        Console.WriteLine("T({0}) brn 0x{1:X} | 0x{2:X}  stack ({3:X})", trackNumber, trkInter.rI[0], trkInter.pc, CallStack.Count);
                        trkInter.jump(trkInter.rI[0]);
                
                        break;
                    case JAISeqEvent.CALL_CONDITIONAL:

                
                        if (checkCondition(  (byte)(trkInter.rI[0] & 15)) )
                        {
                            Console.WriteLine("T({0}) bne 0x{1:X} | 0x{2:X}  stack ({3:X})", trackNumber, trkInter.rI[1], trkInter.pc, CallStack.Count);
                            CallStack.Push(trkInter.pc);
                            trkInter.jump(trkInter.rI[1]);
                        }
                        break;
                    case JAISeqEvent.RETURN:
                        if (CallStack.Count == 0)
                        {
                            Console.WriteLine("Call stack is empty.");
                            crash();
                        }
                        var retaddr = CallStack.Pop();
                        Console.WriteLine("T(0) ret 0x{1:X} | 0x{2:X}  stack ({3:X})", trackNumber, retaddr, trkInter.pc, CallStack.Count);
                        trkInter.jump(retaddr);
                        break;
                    case JAISeqEvent.RETURN_CONDITIONAL:
                        Console.WriteLine("Cond return");
                        if (checkCondition((byte)(trkInter.rI[0] & 15)))
                        {
                            if (CallStack.Count == 0)
                            {
                                Console.WriteLine("Call stack is empty.");
                                crash();
                            }
                            trkInter.jump(CallStack.Pop());
                        }
                        break;
                    case JAISeqEvent.JUMP:
                        //Console.WriteLine(trkInter.rI[0]);
                        trkInter.jump(trkInter.rI[1]);
                        break;
                    case JAISeqEvent.FIN:
                        Console.WriteLine("T(0) [HALT]. ({1} opcode)", trkInter, opcode);
                        halted = true;
                        return;
                    case JAISeqEvent.J2_SET_BANK:
                        Registers[0x20] = (byte)trkInter.rI[0];
                        break;
                    case JAISeqEvent.J2_SET_PROG:
                        Registers[0x21] = (byte)trkInter.rI[0];
                        break;                   
                    case JAISeqEvent.J2_SET_ARTIC:
                        Console.WriteLine("A 0x{0:X} {1}", trkInter.rI[0], trkInter.rI[1]);
                        if (trkInter.rI[0] == 0x62)
                        {
                            JAISeqPlayer.ppqn = trkInter.rI[1];
                            JAISeqPlayer.recalculateTimebase();
                        } else if (trkInter.rI[0]==0x64)
                        {
                            trackArticulation = trkInter.rI[1];
                        }
                        Registers[(byte)trkInter.rI[0]] = (short)trkInter.rI[1];
                        break;  
                    case JAISeqEvent.TIME_BASE:
                        JAISeqPlayer.ppqn = trkInter.rI[0];
                        JAISeqPlayer.recalculateTimebase();
                        break;
                    case JAISeqEvent.J2_TEMPO:
                    case JAISeqEvent.TEMPO:
                        JAISeqPlayer.bpm = trkInter.rI[0];
                        JAISeqPlayer.recalculateTimebase();
                        break;
                    //case JAISeqEvent.SYNC_CPU:
                        //Registers[3] = Convert.ToInt16(Console.ReadLine());

                      
                       // break;
                    case JAISeqEvent.NOTE_ON:
                        {
                    
                            if (muted)
                                continue;
                            var note = trkInter.rI[0];
                            var voice = trkInter.rI[1];
                            var velocity = trkInter.rI[2];
                            var program = Registers[0x21];
                            var bank = Registers[0x20];
                            var ibnks = JaiSeqXLJA.JASystem.Banks;

                            var currentBank = ibnks[bank];
                            if (currentBank == null) { Console.WriteLine("NULL IBNK v{0}",bank); break; }
                            var currentInst = currentBank.Instruments[program];
                            if (currentInst==null) { Console.WriteLine("Empty inst"); break; }
                            var keyNote = currentInst.Keys[note];
                            if (keyNote==null) { Console.WriteLine("EMPTY KEY b{0} p{1} -- n{2} v{3}",bank,program,note,velocity); break; }
                            var keyNoteVel = keyNote.Velocities[velocity];
                            if (keyNoteVel == null) { Console.WriteLine("EMPTY VEL b{0} p{1} -- n{2} v{3}", bank, program, note, velocity); break; }
                            JWave ouData;
                            var snd = JAISeqPlayer.loadSound(keyNoteVel.wsysid, keyNoteVel.wave, out ouData);
                            if (snd == null) { Console.WriteLine("*screams in null wave buffer*",keyNoteVel.wsysid,keyNoteVel.wave); Console.WriteLine(" b{0} p{1} -- n{2} v{3}", bank, program, note, velocity);  break; }
                            
                           // Console.WriteLine($"PC = 0x{trkInter.pc:X6} B = {bank:X} P = {program:X} on N = {note:X} @ V = {velocity:X}");
                            //Console.ReadLine();
                            var newVoice = new JAIDSPVoice(ref snd);
                            var desiredPitch = (float)Math.Pow(2, (note - ouData.key) / 12f) * currentInst.Pitch * keyNoteVel.Pitch * keyNote.Pitch;
                            if (currentInst.IsPercussion == false)
                            {

                                newVoice.setPitchMatrix(0, desiredPitch);
                            } else
                            {
                                //Console.WriteLine($"{trackNumber}@{trkInter.pc} -> PercussionSound -> {note},{velocity}");
                                desiredPitch = 1;
                            }
                            var true_volume =  ((float)velocity / 127f) * currentInst.Volume * keyNoteVel.Volume;
                            //Console.WriteLine($"{currentInst.Volume} | {keyNoteVel.Volume} | {velocity}");

                            newVoice.setVolumeMatrix(0, true_volume* Player.JAISeqPlayer.gainMultiplier )  ;
                            newVoice.setVolumeMatrix(2, volume);
                            if (Registers[7]==2)
                            {
                                newVoice.setPitchMatrix(1, bendFinalValue);
                            }
                            newVoice.tickAdvanceValue = (JAISeqPlayer.timebaseValue) ;


                            if (currentInst.oscillatorCount > 0)
                            {
                                newVoice.setOcillator(currentInst.oscillators[0]);
                                newVoice.tickAdvanceValue =  3*currentInst.oscillators[0].rate;  ; //JAISeqPlayer.ppqn / 30f; ;//(JAISeqPlayer.timebaseValue);//* 0.5f; //* currentInst.oscillators[0].rate; //* 0.5f;
                            }
                 
                            newVoice.play();
                            addVoice(newVoice, (byte)voice);
                            break;
                        }
                    case JAISeqEvent.WRITE_PARENT_PORT:
                        Ports[trkInter.rI[0]] = Registers[(byte)trkInter.rI[1]];
                        break;
                    case JAISeqEvent.NOTE_OFF:
                        if (muted)
                            continue;
                        stopVoice((byte)trkInter.rI[0]);
                        break;
                    case JAISeqEvent.UNKNOWN:       
                        break;
                    case JAISeqEvent.MISS:
                    case JAISeqEvent.LOADTBL:
                        crash();
                        break;
                    default:
                        Console.WriteLine("Trk{0} unimplemented opcode 0x{1:X}({2})",trackNumber,(int)opcode, opcode);
                        break;


                }
     
            }
          
        }
    }
}
