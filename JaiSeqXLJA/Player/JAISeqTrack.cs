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
using System.Diagnostics;

namespace JaiSeqXLJA.Player
{
    public class JAISeqTrack
    {
        JAISeqInterpreter trkInter;
        public JAITrackRegisterMap TrackRegisters = new JAITrackRegisterMap();

        byte[] bmsData;
        int offsetAddr;
        JAISeqInterpreterVersion interVer;

        public Stack<int> CallStack = new Stack<int>(32);
        public int[] Ports = new int[32];
        public int trackNumber;
        public int delay;
        public int lastDelay;
        public float panning;
        public float vibratoDepth = 0;

        public float volume = 1;

        public int looppos = 0;


        JAIDSPVoice[] voices;
        JAIDSPVoice[] voiceOrphans;
        private int trackArticulation = 4;
        public int activeVoices;
        public string lastOpcode;


        public JAISeqTrack parent;

        public bool muted;
        public bool halted;
        public bool crashed;
 

        private static float[] bendCoefficientTable;
        public float pitchBendValue = 1f;
        public float currentVibrato = 1f;
        private JAIDSPLinearSlide pitchBend = new JAIDSPLinearSlide();
        public float pitchTarget;

        
        public float oscW = 1f;
        public float oscR = 1f;
        public float oscV = 1f; 



        public JAISeqTrack(ref byte[] SeqFile, int address, JAISeqInterpreterVersion seqVersion)
        {
            bmsData = SeqFile;
            offsetAddr = address;
            trkInter = new JAISeqInterpreter(ref SeqFile, address, seqVersion);
            voices = new JAIDSPVoice[0xA]; // Even though we only support 7 voices, I can tell that some will linger whenever we stop them.            
            voiceOrphans = new JAIDSPVoice[0xFF];
            interVer = seqVersion;


            TrackRegisters[7] = 12;
        }

        public int pc
        {
            get
            {
                return trkInter.pc;
            }
        }

        public void destroy()
        {
            for (int i = 0; i < voices.Length; i++)
                if (voices[i] != null)
                    voices[i].forceStop();


            for (int i = 0; i < voiceOrphans.Length; i++)
            {
                if (voiceOrphans[i] != null)
                {
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

            this.volume = volume; // * volume;
            for (int i = 0; i < voices.Length; i++)
                if (voices[i] != null)
                    voices[i].setVolumeMatrix(2, this.volume);
        }


        public void updateTrackPanning(float panning)
        {
            this.panning = panning;
            for (int i = 0; i < voices.Length; i++)
                if (voices[i] != null)
                    voices[i].setPanning(panning);

        }
        double lastUpdate = 0;
        private void updateVoices()
        {
            var timeDiffMS = JAISeqPlayer.tickTimer.Elapsed.TotalMilliseconds - lastUpdate;
            lastUpdate = JAISeqPlayer.tickTimer.Elapsed.TotalMilliseconds;

            pitchBend.update();

            var bendSemitones = TrackRegisters[7];
            var bendCalc = ((pitchBend.Value / 8192f) * (bendSemitones)) / 12f;
            pitchBendValue = (float)Math.Pow(2, bendCalc);


            var runtimeSeconds = (JAISeqPlayer.RuntimeMS/ 1000f);
            var tau = (2f * Math.PI);
            currentVibrato = (float)(Math.Sin(6f * tau * runtimeSeconds) * (vibratoDepth / 4096f));  // Semitones 
            var vibratoValue = (float)Math.Pow(2, currentVibrato / 12f); // Semitones to frequency ratio 


           // var oscillator = (float)(Math.Sin(6f * runtimeSeconds * tau * oscR) * oscW) + oscV;




            /*
             * 
            var bendCoef = bendCoefficientTable[Registers[7]];
            var bend = (float)Math.Pow(2, (((float)bendTarget)) / (4096f * bendCoef));
            currentPitchBend = bend;

            */

            for (int i = 0; i < voices.Length; i++)
            {
                if (voices[i] != null)
                {
                    voices[i].setPitchMatrix(1, pitchBendValue);
                    voices[i].setPitchMatrix(2, vibratoValue);
                    voices[i].updateVoice(timeDiffMS);
                }
            }

            for (int i = 0; i < voiceOrphans.Length; i++)
            {
                if (voiceOrphans[i] != null)
                {
                    voiceOrphans[i].setPitchMatrix(1, pitchBendValue);
                    var voiceRes = voiceOrphans[i].updateVoice(timeDiffMS);
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
            // Console.WriteLine("VOICE BUFFER FULL: Trk{0} BA: 0x{1:X} PC: 0x{2:X}\n\nPREPARE FOR THE LEAKENING.", trackNumber, offsetAddr, trkInter.pc);
        }
        private void stopVoice(byte id, bool imm = false)
        {


            if (voices[id] == null)
                return;

            if (id >= voices.Length - 1)
                return;

            if (imm == false)
            {
                voices[id].stop();
                for (int i = 0; i < voiceOrphans.Length; i++)
                {
                    if (voiceOrphans[i] == null)
                    {
                        //Console.WriteLine("found voice orphan buffer.");
                        voiceOrphans[i] = voices[id];
                        break;
                    }
                }
            }
            else
                voices[id].stopImmediately();

            voices[id] = null; // FuuF

            activeVoices--;
            //*/
        }


        private bool checkCondition(byte cond)
        {
            var conditionValue = TrackRegisters[0];
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
           // try
            //{
                realUpdate();
            //}
            //catch (Exception E)
            //{
               // crash();
                //throw E;
            //}
        }
        private void realUpdate()
        {
            updateVoices();

            if (delay > 0) { delay--; }
            if (halted) { return; }
            while (delay < 1 && !halted)
            {
                TrackRegisters[3] = 2;
                var opcode = JAISeqEvent.UNKNOWN;


                try
                {
                    opcode = trkInter.loadNextOp(); // load next operation\

                }
                catch (Exception E)
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
             

                }


                switch (opcode)
                {
            
                    case JAISeqEvent.READPORT:
                        TrackRegisters[0] = 3;
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

                        {
                            if (trkInter.rI[0] == 1)
                            {
                                /*
                                bending = true;
                                bendTargetTicks = trkInter.rI[2];
                                if (bendTargetTicks < 1)
                                    bendTargetTicks = 1;
                         
                                bendTarget = trkInter.rI[1];//opcode == JAISeqEvent.PERF_S16_NODUR ? trkInter.rI[1] / 2 : trkInter.rI[1];
                                bendticks = 0;
                                //Console.WriteLine($"{bendTargetTicks} {bendTarget}");
                                pitchTarget = (bendTarget / (float)0x7FFF) * 0.7f ;
                                */
                                pitchTarget = (trkInter.rI[1] / (float)0x7FFF) * 0.7f;
                                pitchBend.setTarget(trkInter.rI[1], trkInter.rI[2]);

                            }
                            else if (trkInter.rI[0] == 0)
                            {
                                //Console.WriteLine($"{opcode} {trkInter.rI[1]}");
                                volume = trkInter.rF[0];
                                updateTrackVolume((float)volume);
                            }
                            else if (trkInter.rI[0] == 3)
                            {
                                var nintendo = trkInter.rI[1];
                                var fNintendo = (nintendo - 64f) / 64f;
                                updateTrackPanning(-fNintendo);
                            }
                            else if (trkInter.rI[0] == 9)
                            {
                                vibratoDepth = trkInter.rI[1];
                                //Console.WriteLine($"Vibrato depth for {trackNumber} set to {vibratoDepth}");
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
                            var newTrk = new JAISeqTrack(ref bmsData, trkInter.rI[1], interVer);
                            newTrk.trackNumber = trkInter.rI[0];
                            JAISeqPlayer.addTrack(newTrk.trackNumber, newTrk);
                            break;
                        }
                    case JAISeqEvent.J2_SET_PARAM_8:
                    case JAISeqEvent.J2_SET_PARAM_16:
                        {
                            //Console.WriteLine("{0} {1}", trkInter.rI[0], trkInter.rI[1]);
                       
                            TrackRegisters[(byte)trkInter.rI[0]] = (short)trkInter.rI[1];
                            if ((byte)trkInter.rI[0] == 1)
                            {
                                pitchBend.setTarget(trkInter.rI[1], 0);
                                pitchTarget = trkInter.rF[1];
                                Console.WriteLine($"[0x{trkInter.pcl:X5}] mov 0x{trackNumber:X2} 0x{trkInter.rI[0]:X2} 0x{trkInter.rI[1]:X3}");
                            }
                            else if ((byte)trkInter.rI[0] == 0)
                            {
                                //Console.WriteLine(trkInter.rI[1] / 128f);
                                updateTrackVolume(trkInter.rI[1] / 128f);
                                Console.WriteLine($"[0x{trkInter.pcl:X5}] mov 0x{trackNumber:X2} 0x{trkInter.rI[0]:X2} 0x{trkInter.rI[1]:X3}");
                            }
                            else if (trkInter.rI[0] == 3)
                            {
                                var nintendo = trkInter.rI[1];
                                var fNintendo = (nintendo - 64f) / 64f;
                                updateTrackPanning(-fNintendo);
                            } else
                            {
                                Console.WriteLine($"[0x{trkInter.pcl:X5}] mov 0x{trackNumber:X2} 0x{trkInter.rI[0]:X2} 0x{trkInter.rI[1]:X3} ???");
                            }
                            

                            break;
                        }
                    case JAISeqEvent.PARAM_SET_16:
                    case JAISeqEvent.PARAM_SET_8:
                        {
                            TrackRegisters[(byte)trkInter.rI[0]] = (short)trkInter.rI[1];
                            //Console.WriteLine($"PARAM {trkInter.rI[0]} {trkInter.rI[1]}" );
                            if (trkInter.rI[0] == 7)
                                Console.WriteLine($"Pitchbend mode for {trackNumber} to {trkInter.rI[1]}/12");
                            else Console.WriteLine($"[0x{trkInter.pcl:X5}] movp 0x{trackNumber:X2} 0x{trkInter.rI[0]:X2} 0x{trkInter.rI[1]:X3}");



                            break;
                        }
                    case JAISeqEvent.VIBDEPTHMIDI:
                        vibratoDepth = trkInter.rI[0];
                        break;
                    case JAISeqEvent.JUMP_CONDITIONAL:
                        if (checkCondition((byte)(trkInter.rI[0] & 15)))
                        {
                            trkInter.jump(trkInter.rI[1]);
                            Console.WriteLine($"0x{trkInter.pcl:X5} JMP {trackNumber} {trkInter.rI[1]}");
                        }
                        else
                            Console.WriteLine("skip T({0}) jmp C-!> : {1} {2:X} (condition fail)", trackNumber, trkInter.rI[0] & 15, trkInter.rI[1]);
                        break;
                    case JAISeqEvent.CALL:

                        CallStack.Push(trkInter.pc);
                        Console.WriteLine($"[0x{trkInter.pcl:X5}] bl  0x{trackNumber:X2},0x{trkInter.rI[0]:X6} >>> SP=0x{CallStack.Count}");
                        trkInter.jump(trkInter.rI[0]);

                        break;
                    case JAISeqEvent.CALL_CONDITIONAL:


                        if (checkCondition((byte)(trkInter.rI[0] & 15)))
                        {

                            CallStack.Push(trkInter.pc);
                            Console.WriteLine($"[0x{trkInter.pcl:X5}] bl  0x{trackNumber:X2},0x{trkInter.rI[0]:X6} >>> SP=0x{CallStack.Count}");

                            trkInter.jump(trkInter.rI[1]);
                        }
                        break;
                    case JAISeqEvent.RETURN:
                        {
                            if (CallStack.Count == 0)
                            {
                                Console.WriteLine("Call stack is empty.");
                                crash();
                            }
                            var retaddr = CallStack.Pop();
                            Console.WriteLine($"[0x{trkInter.pcl:X5}] blr 0x{trackNumber:X2} >>> PC=0x{retaddr:X4},SP=0x{CallStack.Count}");
                            trkInter.jump(retaddr);
                            break;
                        }
                    case JAISeqEvent.RETURN_CONDITIONAL:

                        if (checkCondition((byte)(trkInter.rI[0] & 15)))
                        {

                            if (CallStack.Count == 0)
                            {
                                Console.WriteLine("Call stack is empty.");
                                crash();
                            }
                            var retaddr = CallStack.Pop();
                            Console.WriteLine($"[0x{trkInter.pcl:X5}] blrc 0x{trackNumber:X2} ? ({trkInter.rI[0]}) >>> PC=0x{retaddr:X4},SP=0x{CallStack.Count}");
                            trkInter.jump(retaddr);
                        }
                        break;
                    case JAISeqEvent.JUMP:
                        Console.WriteLine($"[0x{trkInter.pcl:X5}] jmp 0x{trackNumber:X2},0x{trkInter.rI[0]:X6}");
                        trkInter.jump(trkInter.rI[1]);
                        break;
                    case JAISeqEvent.FIN:
                        Console.WriteLine($"[0x{trkInter.pcl:X5}] HALT 0x{trackNumber:X2}");
                        halted = true;
                        return;
                    case JAISeqEvent.J2_SET_BANK:
                        TrackRegisters[0x20] = (byte)trkInter.rI[0];
                        break;
                    case JAISeqEvent.J2_SET_PROG:
                        TrackRegisters[0x21] = (byte)trkInter.rI[0];
                        break;
                    case JAISeqEvent.J2_SET_ARTIC:
                        Console.WriteLine($"[0x{trkInter.pcl:X5}] movR 0x{trackNumber:X2} 0x{trkInter.rI[0]:X2} 0x{trkInter.rI[1]:X3} R3 = 0x{TrackRegisters[(byte)trkInter.rI[0]]:X}");
                        if (trkInter.rI[0] == 0x62)
                        {
                            JAISeqPlayer.ppqn = trkInter.rI[1];
                            JAISeqPlayer.recalculateTimebase();
                        }
                        else if (trkInter.rI[0] == 0x64)
                        {
                            trackArticulation = trkInter.rI[1];
                        } else if (trkInter.rI[0] == 0x6F)
                        {
                            Console.WriteLine(trkInter.rI[1]);
                            vibratoDepth = (int)((trkInter.rI[1] / 512f) * 4096f);
                        }
                        TrackRegisters[(byte)trkInter.rI[0]] = (short)trkInter.rI[1];
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
                    case JAISeqEvent.SYNC_CPU:
                        /*
                        for (byte ixf = 0; ixf < 7; ixf++) 
                                stopVoice(ixf);
                        */
                        break;
                    case JAISeqEvent.NOTE_ON:
                        {



                            var note = trkInter.rI[0];
                            var voice = trkInter.rI[1];
                            var velocity = trkInter.rI[2];
                            var program = TrackRegisters[0x21];
                            var bank = TrackRegisters[0x20];
                            var ibnks = JaiSeqXLJA.JASystem.Banks;

                            if (muted || ((trackNumber==14 && (note==49 || note==48)) && JAISeqPlayer.noDKJBWhistle))
                            {
                                stopVoice((byte)trkInter.rI[1], true);
                                continue;
                            }

                           


                            var currentBank = ibnks[bank];
                            if (currentBank == null) { var w = Console.ForegroundColor; Console.ForegroundColor = ConsoleColor.Red; Console.Write("[JAISeqTrack - Error] "); Console.ForegroundColor = w; Console.WriteLine("Selected IBNK BNK{0} is NULL", bank); break; }
                            if (program >= currentBank.Instruments.Length) { var w = Console.ForegroundColor; Console.ForegroundColor = ConsoleColor.Red; Console.Write("[JAISeqTrack - Error] "); Console.ForegroundColor = w; Console.WriteLine("Selected PROG PRG{0} is NULL", bank); break; }
                            var currentInst = currentBank.Instruments[program];
                            if (currentInst == null) { var w = Console.ForegroundColor; Console.ForegroundColor = ConsoleColor.Red; Console.Write("[JAISeqTrack - Error] "); Console.ForegroundColor = w; Console.WriteLine("Selected PROG is NULL!"); break; }
                            var keyNote = currentInst.Keys[note];
                            if (keyNote == null) { var w = Console.ForegroundColor; Console.ForegroundColor = ConsoleColor.Red; Console.Write("[JAISeqTrack - Error] "); Console.ForegroundColor = w; Console.WriteLine("BNKPROG Key Empty BNK{0} PRG{1} -- NOT{2} VAL{3}", bank, program, note, velocity); break; }
                            var keyNoteVel = keyNote.Velocities[velocity];
                            if (keyNoteVel == null) { var w = Console.ForegroundColor; Console.ForegroundColor = ConsoleColor.Red; Console.Write("[JAISeqTrack - Error] "); Console.ForegroundColor = w; Console.WriteLine("Velocity empty BANK{0} PRG{1} -- NOT{2} VAL{3}", bank, program, note, velocity); ; break; }
                            JWave ouData;
                            var snd = JAISeqPlayer.loadSound(keyNoteVel.wsysid, keyNoteVel.wave, out ouData);
                            if (snd == null) { var w = Console.ForegroundColor; Console.ForegroundColor = ConsoleColor.Red; Console.Write("[JAISeqTrack - Error] "); Console.ForegroundColor = w; Console.WriteLine("ADPCM Buffer NULL!", keyNoteVel.wsysid, keyNoteVel.wave); Console.WriteLine(" b{0} p{1} -- n{2} v{3}", bank, program, note, velocity); Console.ForegroundColor = ConsoleColor.Yellow; Console.ForegroundColor = w; break; }

                            // Console.WriteLine($"PC = 0x{trkInter.pc:X6} B = {bank:X} P = {program:X} on N = {note:X} @ V = {velocity:X}");
                            //Console.ReadLine();
                            var newVoice = new JAIDSPVoice(ref snd);



                            var desiredPitch = (float)Math.Pow(2, ((note - ouData.key)) / 12f) * currentInst.Pitch * keyNoteVel.Pitch * keyNote.Pitch;
                            if (currentInst.IsPercussion == true)
                            {                               //Console.WriteLine($"{trackNumber}@{trkInter.pc} -> PercussionSound -> {note},{velocity}");
                                desiredPitch = currentInst.Pitch * keyNoteVel.Pitch * keyNote.Pitch;
                            }
                            newVoice.setPitchMatrix(0, desiredPitch);
                            var fVel = ((float)velocity / 127f);
                 
                            var true_volume = fVel * currentInst.Volume * keyNoteVel.Volume * keyNote.Volume;
                            true_volume *= Player.JAISeqPlayer.gainMultiplier;

                            newVoice.setVolumeMatrix(0,  true_volume );
                            newVoice.setVolumeMatrix(2, volume);

                            newVoice.setPanning(panning);
                            newVoice.setPitchMatrix(1, pitchBendValue);



                            newVoice.tickAdvanceValue = (JAISeqPlayer.timebaseValue);

                   
                            if (currentInst.oscillatorCount > 0)
                            {
                                var osc = currentInst.oscillators[0];
                                newVoice.setOcillator(currentInst.oscillators[0]);
                            }

                        
                            newVoice.play(trackNumber==9);
                            addVoice(newVoice, (byte)voice);
                            break;
                        }
                    case JAISeqEvent.WRITE_PARENT_PORT:
                        Ports[trkInter.rI[0]] = TrackRegisters[(byte)trkInter.rI[1]];
                        break;
                    case JAISeqEvent.NOTE_OFF:
                        {
                            var program = TrackRegisters[0x21];
                            var bank = TrackRegisters[0x20];
                            var ibnks = JaiSeqXLJA.JASystem.Banks;

                            var currentBank = ibnks[bank];
                            if (currentBank == null || (program >= currentBank.Instruments.Length)) { var w = Console.ForegroundColor; Console.ForegroundColor = ConsoleColor.Red; Console.Write("[JAISeqTrack - Error] "); Console.ForegroundColor = w; Console.WriteLine("Selected IBNK BNK{0} is NULL", bank); break; }
                           
                            var currentInst = currentBank.Instruments[program];

                            var perc = false;
                            if (currentInst != null)
                                perc = currentInst.IsPercussion;

                            if (muted)
                                continue;
                            stopVoice((byte)trkInter.rI[0], perc);
                            break;
                        }
                    case JAISeqEvent.UNKNOWN:
                        break;
                    case JAISeqEvent.CLOSE_TRACK:

                        break;
                    case JAISeqEvent.MISS:
                    case JAISeqEvent.LOADTBL:
                        crash();
                        break;
                    default:
                        var ww = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("E: ");
                        Console.WriteLine("Trk{0} unimplemented opcode 0x{1:X}({2}) @ {3:X}", trackNumber, (int)opcode, opcode, trkInter.pc);
                        Console.ForegroundColor = ww;
                        continue;
                        break;


                }

            }

        }
    }
}
