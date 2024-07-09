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
using Un4seen.Bass;

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
        public float panning = 64f;
        public float vibratoDepth = 0;

        public float volume = 1;
        public float reverb = 0f;

        public int looppos = 0;


        JAIDSPVoice[] voices;
        JAIDSPVoice[] voiceOrphans;
        private int trackArticulation = 4;
        public int activeVoices;
        public int activeVoiceOrphans;
        public string lastOpcode;
        private float uhoh = 0f;


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

        public static Random wxxxx;


        public JAISeqTrack(ref byte[] SeqFile, int address, JAISeqInterpreterVersion seqVersion)
        {
            bmsData = SeqFile;
            offsetAddr = address;
            trkInter = new JAISeqInterpreter(ref SeqFile, address, seqVersion);
            voices = new JAIDSPVoice[0xA]; // Even though we only support 7 voices, I can tell that some will linger whenever we stop them.            
            voiceOrphans = new JAIDSPVoice[0xFF];
            interVer = seqVersion;
            if (wxxxx == null)
                wxxxx = new Random(DateTime.Now.Second);

            TrackRegisters[7] = 12;
        }

        private void error(string function, string data)
        {
            
           // var w = Console.ForegroundColor; Console.ForegroundColor = ConsoleColor.Red; Console.Write($"JAISeqTrack::{function} > "); Console.ForegroundColor = w;
           // Console.WriteLine(data);
         

        }
        private void error(string function, string data, params object[] format)
        {
           
           // var w = Console.ForegroundColor; Console.ForegroundColor = ConsoleColor.Red; Console.Write($"JAISeqTrack::{function} > "); Console.ForegroundColor = w;
           // Console.WriteLine(data,format);
            
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
                if (voiceOrphans[i] != null)
                    voiceOrphans[i].forceStop();
        }

        public void purgeVoices()
        {
            for (int i = 0; i < voices.Length; i++)
                if (voices[i] != null)
                    voices[i].forceStop();
            
        }

        public float Lerp(float firstFloat, float secondFloat, float by)
        {
            return firstFloat * (1 - by) + secondFloat * by;
        }

        public void updateTrackVolume(float volume)
        {

            this.volume = volume * volume;
            for (int i = 0; i < voices.Length; i++)
                if (voices[i] != null)
                    voices[i].setVolumeMatrix(2, this.volume);

            for (int i = 0; i < voiceOrphans.Length; i++)
                if (voiceOrphans[i] != null)
                    voiceOrphans[i].setVolumeMatrix(2, this.volume);
        }

        public void updateTrackReverb(float reverb)
        {
            this.reverb = reverb;
            for (int i = 0; i < voices.Length; i++)
                if (voices[i] != null)
                    voices[i].setReverb(reverb);

            for (int i = 0; i < voiceOrphans.Length; i++)
                if (voiceOrphans[i] != null)
                    voiceOrphans[i].setReverb(reverb);
        }

        public void updateTrackPanning(float panning)
        {

            var fp = (64f - panning) + 64f;

            this.panning = fp;
            for (int i = 0; i < voices.Length; i++)
                if (voices[i] != null)
                    voices[i].setPanMatrix(0, fp);

            for (int i = 0; i < voiceOrphans.Length; i++)
                if (voiceOrphans[i] != null)
                    voiceOrphans[i].setPanMatrix(0, fp);
        }
        double lastUpdate = 0;
        double timeDiffMS = 0;
        private void updateVoices()
        {
            timeDiffMS = JAISeqPlayer.tickTimer.Elapsed.TotalMilliseconds - lastUpdate;
            lastUpdate = JAISeqPlayer.tickTimer.Elapsed.TotalMilliseconds;

            pitchBend.update();

            var bendSemitones = TrackRegisters[7];
            var bendCalc = ((pitchBend.Value / 8192f) * (bendSemitones)) / 12f;
            pitchBendValue = (float)Math.Pow(2, bendCalc);

            
            var runtimeSeconds = (JAISeqPlayer.RuntimeMS/ 1000f);
            var tau = (2f * Math.PI);
            currentVibrato = (float)(Math.Cos(6f * tau * runtimeSeconds) * (vibratoDepth / 4096f));  // Semitones 
            var vibratoValue = (float)Math.Pow(2, currentVibrato / 12f); // Semitones to frequency ratio 

            for (int i = 0; i < voices.Length; i++)
            {
                if (voices[i] != null)
                {
                    voices[i].setPitchMatrix(1, pitchBendValue + uhoh);
                    voices[i].setPitchMatrix(2, vibratoValue);
                    if (voices[i].updateVoice(timeDiffMS)==3)
                        voices[i].stop(); 
                }
            }

            for (int i = 0; i < voiceOrphans.Length; i++)
                if (voiceOrphans[i] != null)
                {
                    // kill the orphans when necessary
                    voiceOrphans[i].setPitchMatrix(1, pitchBendValue + uhoh);
                    voiceOrphans[i].setPitchMatrix(2, vibratoValue);
                    if (voiceOrphans[i].updateVoice(timeDiffMS) == 3)
                    {
                        voiceOrphans[i] = null;
                        activeVoiceOrphans--;
                    }
                }
        }

        private void addVoice(JAIDSPVoice voice, byte id)
        {
            activeVoices++;
            stopVoice(id);
            voices[id] = voice;
        }
        private void stopVoice(byte id, bool imm = false)
        {


            if (voices[id] == null)
                return;

            if (id >= voices.Length - 1)
                return;
         
            voices[id].stop();

            for (int i = 0; i < voiceOrphans.Length; i++)
                if (voiceOrphans[i] == null)
                {
                    voiceOrphans[i] = voices[id];
                    activeVoiceOrphans++;
                    break;
                }

            voices[id] = null;
            activeVoices--;

        }


        private bool checkCondition(byte cond)
        {
            var conditionValue = TrackRegisters[0];
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

                realUpdate();
    
        }
        private void realUpdate()
        {
            updateVoices();

            if (delay > 0) { delay--; }
            if (halted) { return; }
            while (delay <= 0 && !halted)
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
                    lastOpcode = $"{(int)opcode:x2}-{opcode}";
             

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
                                //Console.WriteLine($"{opcode} v={trkInter.rI[1]} t={trkInter.rI[2]} a=0x{pc:X}");
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
                                //var fNintendo = (nintendo - 64f) / 64f;
         
                                updateTrackPanning(nintendo);
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
                                //Console.WriteLine($"[T{trackNumber:X2}@0x{trkInter.pcl:X5}] pitchbend to {trkInter.rI[1]}");
                            }
                            else if ((byte)trkInter.rI[0] == 0)
                            {
                                //Console.WriteLine(trkInter.rI[1] / 128f);
                                updateTrackVolume(trkInter.rI[1] / 128f);
                                //Console.WriteLine($"[T{trackNumber:X2}@0x{trkInter.pcl:X5}] volume to {trkInter.rI[1]}");
                            } else if (trkInter.rI[0]==2)
                            {
                                updateTrackReverb( (trkInter.rI[1] / 128f));
                                //Console.WriteLine($"!!!!!!!![T{trackNumber:X2}@0x{trkInter.pcl:X5}] reverb to {trkInter.rI[1]}");
                            }
                            else if (trkInter.rI[0] == 3)
                            {
                                var nintendo = ( 64f - trkInter.rI[1]) + 64f;
                                //var fNintendo = (nintendo - 64f) / 64f;
                                //Console.WriteLine(opcode / 128f);
                                updateTrackPanning(nintendo);
                            } else
                            {
                                //Console.WriteLine($"[T{trackNumber:X2}@0x{trkInter.pcl:X5}] set f-prm {trkInter.rI[0]} to 0x{trkInter.rI[1]:X3} ???");
                            }
                            

                            break;
                        }
                    case JAISeqEvent.PARAM_SET_16:
                    case JAISeqEvent.PARAM_SET_8:
                        {
                            TrackRegisters[(byte)trkInter.rI[0]] = (short)trkInter.rI[1];
                            Console.WriteLine($"PARAM {trkInter.rI[0]} {trkInter.rI[1]}" );
                            if (trkInter.rI[0] == 7)
                                Console.WriteLine($"[T{trackNumber:X2}@0x{trkInter.pcl:X5}] bend octaves to {trkInter.rI[1]} ");
                            else Console.WriteLine($"[T{trackNumber:X2}@0x{trkInter.pcl:X5}] iprm {trkInter.rI[0]} to 0x{trkInter.rI[1]:X3}");


                      
                            break;
                        }
                    case JAISeqEvent.VIBDEPTHMIDI:
                        vibratoDepth = trkInter.rI[0];
                        break;
                    case JAISeqEvent.JUMP_CONDITIONAL:
                        if (checkCondition((byte)(trkInter.rI[0] & 15)))
                        {
                            trkInter.jump(trkInter.rI[1]);
                            Console.WriteLine($"[T{trackNumber:X2}@0x{trkInter.pcl:X5}] jumps to 0x{trkInter.rI[1]:X6}");
                        }
                        else
                            Console.WriteLine("skip T({0}) jmp C-!> : {1} {2:X} (condition fail)", trackNumber, trkInter.rI[0] & 15, trkInter.rI[1]);
                        break;
                    case JAISeqEvent.CALL:

                        CallStack.Push(trkInter.pc);
                        Console.WriteLine($"[T{trackNumber:X2}@0x{trkInter.pcl:X5}] calls 0x{trkInter.rI[0]:X6} StackDepth={CallStack.Count}");
                        trkInter.jump(trkInter.rI[0]);

                        break;
                    case JAISeqEvent.CALL_CONDITIONAL:


                        if (checkCondition((byte)(trkInter.rI[0] & 15)))
                        {

                            CallStack.Push(trkInter.pc);
                            Console.WriteLine($"[T{trackNumber:X2}@0x{trkInter.pcl:X5}] calls 0x{trkInter.rI[1]:X6} StackDepth={CallStack.Count}");

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
                            Console.WriteLine($"[T{trackNumber:X2}@0x{trkInter.pcl:X5}] returns to 0x{retaddr:X4} StackDepth=0x{CallStack.Count}");
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
                            Console.WriteLine($"[T{trackNumber:X2}@0x{trkInter.pcl:X5}] returns if (R3=={trkInter.rI[0]}) to 0x{retaddr:X4} StackDepth={CallStack.Count}");
                            trkInter.jump(retaddr);
                        }
                        break;
                    case JAISeqEvent.JUMP:
                        Console.WriteLine($"[T{trackNumber:X2}@0x{trkInter.pcl:X5}] jumps to 0x{trkInter.rI[0]:X6}");
                        trkInter.jump(trkInter.rI[1]);
                        break;
                    case JAISeqEvent.FIN:
                        Console.WriteLine($"[T{trackNumber:X2}@0x{trkInter.pcl:X5}] HALTS.");
                        halted = true;
                        return;
                    case JAISeqEvent.J2_SET_BANK:
                        TrackRegisters[0x20] = (byte)trkInter.rI[0];
                        break;
                    case JAISeqEvent.J2_SET_PROG:
                        TrackRegisters[0x21] = (byte)trkInter.rI[0];
                        break;
                    case JAISeqEvent.J2_SET_ARTIC:
                        Console.WriteLine($"[T{trackNumber:X2}@0x{trkInter.pcl:X5}] sets parameter {trkInter.rI[0]} to 0x{trkInter.rI[1]:X3} R3 = 0x{TrackRegisters[(byte)trkInter.rI[0]]:X}");
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
                    case JAISeqEvent.VIBRATO_PITCH:
                        Console.WriteLine($"Vibpitch {trkInter.rI[0]}");
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
                            if (currentBank == null) {  error("noteOn","Selected IBNK BNK{0} is NULL", bank); break; }
                            if (program >= currentBank.Instruments.Length) { error("noteOn", "Selected PROG PRG{0} is NULL", bank); break; }
                            var currentInst = currentBank.Instruments[program];
                            if (currentInst == null) { error("noteOn", "Selected PROG is NULL!"); break; }
                            var keyNote = currentInst.Keys[note];
                            if (keyNote == null) {error("noteOn", "BNKPROG Key Empty BNK{0} PRG{1} -- NOT{2} VAL{3}", bank, program, note, velocity); break; }
                            var keyNoteVel = keyNote.Velocities[velocity];
                            if (keyNoteVel == null) { error("noteOn", "Velocity empty BANK{0} PRG{1} -- NOT{2} VAL{3}", bank, program, note, velocity); ; break; }
                            JWave ouData;
                            var snd = JAISeqPlayer.loadSound(keyNoteVel.wsysid, keyNoteVel.wave, out ouData);
                            if (snd == null) { error("noteOn", "ADPCM Buffer NULL!", keyNoteVel.wsysid, keyNoteVel.wave); Console.WriteLine(" b{0} p{1} -- n{2} v{3}", bank, program, note, velocity); break; }

    
                            var newVoice = new JAIDSPVoice(ref snd);

                            //if (trackNumber == 3)
                                //Console.WriteLine($"VOL {volume} VEL {velocity} kn{keyNote.Volume} knv{keyNoteVel.Volume} ci{currentInst.Volume}");
                         

                            var desiredPitch = (float)Math.Pow(2, ((note - ouData.key)) / 12f) * currentInst.Pitch * keyNoteVel.Pitch * keyNote.Pitch;
                            if (currentInst.IsPercussion == true)
                                desiredPitch = currentInst.Pitch * keyNoteVel.Pitch * keyNote.Pitch;
                            
                            newVoice.setPitchMatrix(0, desiredPitch);
                            var fVel = ((float)velocity / 127f);
                            fVel *= (fVel * currentInst.Volume * keyNoteVel.Volume * keyNote.Volume);
                            var true_volume = fVel ;
                            true_volume *= Player.JAISeqPlayer.gainMultiplier;

                            newVoice.setVolumeMatrix(0,  true_volume );
                            newVoice.setVolumeMatrix(2, volume);

                            newVoice.setPanMatrix(0,panning);
                            newVoice.setPanMatrix(1,keyNote.Pan);

                            newVoice.setPitchMatrix(1, pitchBendValue);
                            newVoice.setReverb(reverb);
                          
                   
                            if (currentInst.oscillatorCount > 0)
                                newVoice.setOcillator(currentInst.oscillators[0]);

                            //if (currentInst.oscillatorCount > 1)
                            //    Console.WriteLine($"Unsupported multi-oscillator instrument :( {currentInst.oscillators[1].target}");
                            
                            newVoice.play();
                     
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
                            if (currentBank == null || (program >= currentBank.Instruments.Length)) { error("noteOff", "Selected IBNK BNK{0} is NULL", bank); break; }
                           
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

                        var ww = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("E: ");
                        Console.WriteLine("Trk{0} unknown opcode 0x{1:X}({2}) @ {3:X}", trackNumber, trkInter.last_opcode, (JAISeqEvent)trkInter.last_opcode, trkInter.pc);
                        Console.ForegroundColor = ww;
                        //crash();

                        break;
                    case JAISeqEvent.CLOSE_TRACK:

                        break;
                    case JAISeqEvent.PARAM_SET_R:
                        TrackRegisters[(byte)trkInter.rI[1]] = TrackRegisters[(byte)trkInter.rI[0]];
                        break;

                    case JAISeqEvent.LOADTBL:
                        if (trkInter.rI[0] == 0x20)
                        {
                            var dest_register = trkInter.rI[1]  ;
                            var address_register = trkInter.rI[2]  ;
                            var relative_register = trkInter.rI[3] ;
                            var reader = trkInter.Sequence;
                            // This codebase is too old, I just want to see if this works. 

                            var old_pos = reader.BaseStream.Position;
                          
                            reader.BaseStream.Position = TrackRegisters[(byte)address_register] + 3 * TrackRegisters[(byte)relative_register];
                            Console.WriteLine($"[{old_pos:X}]{trackNumber} Reading from 0x{reader.BaseStream.Position:X}");
                            TrackRegisters[(byte)dest_register] = (short)Helpers.ReadUInt24BE(trkInter.Sequence);

                            Console.WriteLine($"Loaded address 0x{TrackRegisters[(byte)dest_register]:X} ");
                            reader.BaseStream.Position = old_pos;
                     
                            break;
                  
                        }
                        crash();
                        break;
                    case JAISeqEvent.OVERRIDE_1:
                        {
                            if (trkInter.rI[0] != 0xC1) { crash(); break; }
                            if (trkInter.rI[1] != 0x40 ) { crash(); break; }
                            var trackID = trkInter.rI[2];
                            var data = TrackRegisters[(byte)trkInter.rI[3]];

                            var newTrk = new JAISeqTrack(ref bmsData, data, interVer);
                            newTrk.trackNumber = trackID;
                            Console.WriteLine($"OVERRIDE {trackID} New track at 0x{data:X}");
                            JAISeqPlayer.addTrack(newTrk.trackNumber, newTrk);
                     
                            break;
                        }
                    case JAISeqEvent.WAIT_REGISTER:
                        //crash();
                        delay += 0xFF;
                        break;
                    case JAISeqEvent.MISS:
                        crash();
                        break;
                    default:
                        ww = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("E: ");
                        Console.WriteLine("Trk{0} unimplemented opcode 0x{1:X}({2}) @ {3:X}", trackNumber, (int)opcode, opcode, trkInter.pc);
                        Console.ForegroundColor = ww;
                        break;
                 

                }

            }

        }
    }
}
