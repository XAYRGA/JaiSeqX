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
using System.Runtime.InteropServices;

namespace JaiSeqXLJA.Player
{
    public class JAISeqTrack
    {
        JAISeqInterpreter trkInter;

        public Dictionary<int, JAISeqTrack> Children = new Dictionary<int,JAISeqTrack>(16);
        public JAISeqTrack Parent;
        public string TrackName = "ROOT TRACK";

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
        public bool[] voiceStatus = new bool[8];


        public int transpose = 0;
        public float volume = 1;
        public float reverb = 0f;

        public int looppos = 0;
        public bool interrupt_pause = false;


        JAIDSPVoice[] voices;
        JAIDSPVoice[] voiceOrphans;
        private int trackArticulation = 4;
        public int activeVoices;
        public int activeVoiceOrphans;
        public string lastOpcode;
        private float uhoh = 0f;
        public float vibPitch = 12f;

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
            
            var w = Console.ForegroundColor; Console.ForegroundColor = ConsoleColor.Red; Console.Write($"JAISeqTrack::{function} > "); Console.ForegroundColor = w;
            Console.WriteLine(data);
         

        }
        private void error(string function, string data, params object[] format)
        {
           
            var w = Console.ForegroundColor; Console.ForegroundColor = ConsoleColor.Red; Console.Write($"JAISeqTrack::{function} > "); Console.ForegroundColor = w;
            Console.WriteLine(data,format);
            
        }
    

        public int pc
        {
            get
            {
                return trkInter.pc;
            }
        }

        public void destroyChildren()
        {
            List<int> ChildrenToDestroy = new List<int>();
            foreach (KeyValuePair<int,JAISeqTrack> child in Children)
            {
                child.Value.destroy();
                ChildrenToDestroy.Add(child.Key);
            }

            foreach (int child in ChildrenToDestroy)
                Children.Remove(child);
        }

        public void destroy()
        {
            for (int i = 0; i < voices.Length; i++)
                if (voices[i] != null)
                    voices[i].forceStop();


            for (int i = 0; i < voiceOrphans.Length; i++)
                if (voiceOrphans[i] != null)
                    voiceOrphans[i].forceStop();

            destroyChildren();
     
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


        int _syncval = 0;
        bool syncval_written = false;
        public void writeSyncValue(byte value)
        {
            _syncval = value;
            syncval_written = true;
       
            foreach (KeyValuePair<int, JAISeqTrack> pair in Children)
                pair.Value.writeSyncValue(value);
        }

        public void clearInterrupt()
        {
            interrupt_pause = false;
            foreach (KeyValuePair<int, JAISeqTrack> pair in Children)
                pair.Value.clearInterrupt();
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
            var vibratoValue = (float)Math.Pow(2, currentVibrato / vibPitch); // Semitones to frequency ratio 

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
            voiceStatus[id] = true;
        }
        private void stopVoice(byte id, bool imm = false)
        {


            if (voices[id] == null)
                return;

            if (id >= voices.Length - 1)
                return;
         
            voices[id].stop();
            voiceStatus[id] = false;

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
            var conditionValue = TrackRegisters[3];

            switch (cond)
            {
                case 0:
                    //Console.WriteLine($"0x{pc:x} CHECK CONDITION ON {conditionValue} none");
                    return true;  // oops, all boolean
                case 1: 
                    //Console.WriteLine($"0x{pc:x} CHECK CONDITION ON {conditionValue}==0?");
                    if (conditionValue == 0) { return true; }
                    return false;
                case 2:
                    //Console.WriteLine($"0x{pc:x} CHECK CONDITION ON {conditionValue}!=0");
                    if (conditionValue != 0) { return true; }
                    return false;
                case 3: 
                   // Console.WriteLine($"0x{pc:x} CHECK CONDITION ON {conditionValue}==1");
                    if (conditionValue >= 0) { return true; }
                    return false;
                case 4: 
                    //Console.WriteLine($"0x{pc:x} CHECK CONDITION ON {conditionValue} > 0");
                    if (conditionValue <= 0) { return true; }
                    return false;
                case 5:
                    //Console.WriteLine($"0x{pc:x} CHECK CONDITION ON {conditionValue}<0");
                    if (conditionValue > 0) { return true; }
                    return false;
            }
            return false;
        }

        public void crash()
        {

            Console.WriteLine($"[!] Track {TrackName} crashed at {pc}");
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
            try
            {
                realUpdate();
            } catch (Exception E)
            {
                error("update", $"oops: {E.ToString()}");
                crash();
            }


            foreach (KeyValuePair<int, JAISeqTrack> child2 in Children)
            {

                var child = child2.Value;
                    if (child != null)
                        try
                        {
                            child.update();
                        }
                        catch { Console.Write("cannot update child"); }
                
            }
     
             
    
        }

        private bool getTrackMuteArg(string tid)
        {
            var muteIdx = JaiSeqXLJA.findDynamicStringArgument("-mute", "none").Split(',');
            for (int i = 0; i < muteIdx.Length; i++)
                if (muteIdx[i] != null && muteIdx[i] == tid)
                    return true;
        
            return false;
        }

        public void writePort(int port, short data)
        {
            Ports[port] = data;
            TrackRegisters[3] = data;
            if (Parent!= null)            
                Parent.writePort(port, data);
            Console.WriteLine($"{TrackName} wrote port {port}={data}");
        }
        private void realUpdate()
        {
            updateVoices();

            
            if (delay > 0) { delay--; }
            if (interrupt_pause)
                return;
            if (halted) { return; }
            while (delay <= 0 && !halted && !interrupt_pause)
            {
    
                var opcode = JAISeqEvent.UNKNOWN;
                if (CallStack.Count > 16)
                {
                    Console.WriteLine("Stack overflow!");
                    crash();
                    break;
                }


                try
                {
                    opcode = trkInter.loadNextOp(); // load next operation\

                }
                catch (Exception E)
                {
                    Console.WriteLine("Track {0} C# exception crash", TrackName);
                    crash();
                    halted = true;
                    Console.WriteLine(E.ToString());
                    return;
                }

                if (opcode != JAISeqEvent.WAIT_8 && opcode != JAISeqEvent.WAIT_16 && opcode != JAISeqEvent.WAIT_VAR) //&& opcode!=JAISeqEvent.NOTE_OFF && opcode!=JAISeqEvent.NOTE_ON) 
                {
                  lastOpcode = $"{(int)opcode:x2}-{opcode}";
                   //if (trackNumber==0 && parent==JAISeqPlayer.RootTrack) 
                       // Console.WriteLine($"{pc:X} ({trackNumber}) @ {opcode} ");
             

                }


                switch (opcode)
                {
            
                    case JAISeqEvent.READPORT:
                        TrackRegisters[(byte)trkInter.rI[1]] = (short)Ports[trkInter.rI[0]];
                        TrackRegisters[3] = (short)Ports[trkInter.rI[0]];

                
                        if (this!=JAISeqPlayer.RootTrack)
                        {
                            JaiSeqXLJA.sequenceTransitioning = false;
                            Console.WriteLine($"[{TrackName}@0x{pc:x}] rpt {trkInter.rI[0]:X},{trkInter.rI[1]} = {TrackRegisters[3]}");
                        }
                        //Console.WriteLine($"[{TrackName}@0x{pc:x}] readp {trkInter.rI[0]:X},{trkInter.rI[1]} = {TrackRegisters[3]}");
                        break;
                    case JAISeqEvent.WRITEPORT:
                        var portValue = TrackRegisters[(byte)trkInter.rI[1]];
                        writePort(trkInter.rI[0], portValue);
                        Console.WriteLine($"[{TrackName}@0x{pc:x}] writep {trkInter.rI[0]:X},{trkInter.rI[1]} = {TrackRegisters[3]}");
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
                            var trackID = trkInter.rI[0];
                            newTrk.trackNumber = trackID;
                            newTrk.Parent = this;

                            var tid = Parent == null ? $"{newTrk.trackNumber}" : $"{this.trackNumber}.{trackID}";
                            newTrk.TrackName = Parent == null ? $"Track {newTrk.trackNumber}" : $"Child {this.trackNumber}.{trackID}";
                            newTrk.muted = getTrackMuteArg(tid);
                            if (Children.ContainsKey(trackID))
                            {
                                Children[trackID].destroy();
                                Children.Remove(trackID);
                            }
                    
                            Children.Add(trackID, newTrk);

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
                                //Console.WriteLine($"[{TrackName}@0x{trkInter.pcl:X5}] pitchbend to {trkInter.rI[1]}");
                            }
                            else if ((byte)trkInter.rI[0] == 0)
                            {
                                //Console.WriteLine(trkInter.rI[1] / 128f);
                                updateTrackVolume(trkInter.rI[1] / 128f);
                                //Console.WriteLine($"[{TrackName}@0x{trkInter.pcl:X5}] volume to {trkInter.rI[1]}");
                            } else if (trkInter.rI[0]==2)
                            {
                                updateTrackReverb( (trkInter.rI[1] / 128f));
                                //Console.WriteLine($"!!!!!!!![{TrackName}@0x{trkInter.pcl:X5}] reverb to {trkInter.rI[1]}");
                            }
                            else if (trkInter.rI[0] == 3)
                            {
                                var nintendo = ( 64f - trkInter.rI[1]) + 64f;
                                //var fNintendo = (nintendo - 64f) / 64f;
                                //Console.WriteLine(opcode / 128f);
                                updateTrackPanning(nintendo);
                            } else
                            {
                                //Console.WriteLine($"[{TrackName}@0x{trkInter.pcl:X5}] set f-prm {trkInter.rI[0]} to 0x{trkInter.rI[1]:X3} ???");
                            }
                            

                            break;
                        }
                    case JAISeqEvent.PARAM_SET_16:
                    case JAISeqEvent.PARAM_SET_8:
                        {
                            TrackRegisters[(byte)trkInter.rI[0]] = (short)trkInter.rI[1];
                            Console.WriteLine($"PARAM {trkInter.rI[0]} {trkInter.rI[1]}" );
                            if (trkInter.rI[0] == 7)
                                Console.WriteLine($"[{TrackName}@0x{trkInter.pcl:X5}] bend octaves to {trkInter.rI[1]} ");
                            else Console.WriteLine($"[{TrackName}@0x{trkInter.pcl:X5}] iprm {trkInter.rI[0]} to 0x{trkInter.rI[1]:X3}");


                      
                            break;
                        }
                    case JAISeqEvent.VIBDEPTHMIDI:
                        vibratoDepth = trkInter.rI[0];
                        break;
                    case JAISeqEvent.JUMP_CONDITIONAL:
                        var flg = trkInter.rI[0];
                        //Console.WriteLine($"{pc:X} {flg & 0xF}, {flg >> 8}");
                       
                        if (checkCondition((byte)(trkInter.rI[2])))
                        {
                            trkInter.jump(trkInter.rI[1]);
                            if (TrackName!="ROOT TRACK")
                                Console.WriteLine($"[{TrackName}@0x{trkInter.pcl:X5}] jumps to 0x{trkInter.rI[1]:X6}");
                        }
                        //else
                            //Console.WriteLine("skip T({0}) jmp C-!> : {1} {2:X} (condition fail)", trackNumber, trkInter.rI[0] & 15, trkInter.rI[1]);
                        break;
                    case JAISeqEvent.CALL:

                        CallStack.Push(trkInter.pc);
                        Console.WriteLine($"[{TrackName}@0x{trkInter.pcl:X5}] calls 0x{trkInter.rI[0]:X6} StackDepth={CallStack.Count}");
                        trkInter.jump(trkInter.rI[0]);

                        break;
                    case JAISeqEvent.CALL_CONDITIONAL:
                        var cond = (byte)trkInter.rI[0];
                        var modifier = (cond >> 4) & 0xF;
                        var condMode = (byte)(cond & 0xF);
                        var address = trkInter.rI[1];
                        var secondaryIndexRegister = TrackRegisters[0];


                        if (checkCondition(condMode))
                        {
                            CallStack.Push(trkInter.pc);

                            if (modifier == 0xC)
                            {
                                var reader = trkInter.Sequence;
                                var old_pos = reader.BaseStream.Position;
                                var old_addr = address;
                                var regVal = 0; //TrackRegisters[registerTarget];
                                var index = TrackRegisters[4];
                                if (index < 0)
                                {
                                    error("CALL_CONDITIONAL", $"Attempt to jump to negative index {index}");
                                    break;
                                }
                                reader.BaseStream.Position = address + 3 * (index + secondaryIndexRegister);
                                address = (int)Helpers.ReadUInt24BE(reader);
                                reader.BaseStream.Position = old_pos;
                                Console.WriteLine($"[{TrackName}@0x{trkInter.pcl:X5}] calltable 0x{old_addr:X} idx={index} to 0x{address:X}");
                                if (this == JAISeqPlayer.RootTrack)
                                    JaiSeqXLJA.sequenceTransitioning = false;
                            } else 
                                Console.WriteLine($"[{TrackName}@0x{trkInter.pcl:X5}] calls 0x{trkInter.rI[1]:X6} StackDepth={CallStack.Count}");

                            trkInter.jump(address);
                        }


                        break;
                    case JAISeqEvent.RETURN:
                        {
                            if (CallStack.Count == 0)
                            {
                                Console.WriteLine("!!!!!!!!!Call stack is empty.");
                                crash();
                            }
                            var retaddr = CallStack.Pop();
                            Console.WriteLine($"[{TrackName}@0x{trkInter.pcl:X5}] returns to 0x{retaddr:X4} StackDepth=0x{CallStack.Count}");
                            trkInter.jump(retaddr);
                            break;
                        }
                    case JAISeqEvent.RETURN_CONDITIONAL:

                       
                        if (checkCondition((byte)(trkInter.rI[0])))
                        {
                            if (CallStack.Count == 0)
                            {
                                Console.WriteLine("!!!!!!!!!Call stack is empty.");
                                crash();
                            }
                            var retaddr = CallStack.Pop();
                            Console.WriteLine($"[{TrackName}@0x{trkInter.pcl:X5}] returns if (R3=={trkInter.rI[0]}) to 0x{retaddr:X4} StackDepth={CallStack.Count}");
                            trkInter.jump(retaddr);
                        }
                        break;
                    case JAISeqEvent.JUMP:
                        if (TrackName!="ROOT TRACK")
                            Console.WriteLine($"[{TrackName}@0x{trkInter.pcl:X5}] jumps to 0x{trkInter.rI[1]:X6}");
                        trkInter.jump(trkInter.rI[1]);
                        break;
                    case JAISeqEvent.FIN:
                        Console.WriteLine($"[{TrackName}@0x{trkInter.pcl:X5}] HALTS.");                        
                        halted = true;
                        destroyChildren();                      
                        return;
                    case JAISeqEvent.J2_SET_BANK:
                        TrackRegisters[0x20] = (byte)trkInter.rI[0];
                        break;
                    case JAISeqEvent.J2_SET_PROG:
                        TrackRegisters[0x21] = (byte)trkInter.rI[0];
                        break;
                    case JAISeqEvent.J2_READPORT:
                        TrackRegisters[(byte)trkInter.rI[1]] = (short)Ports[trkInter.rI[0]];
                        //Console.WriteLine($"READING PORT {trkInter.rI[0]} into reg {trkInter.rI[1]} v={Ports[trkInter.rI[0]]}");
                        TrackRegisters[3] = (short)Ports[trkInter.rI[0]]; // Apparently reads get written to r3
                        break;
                    case JAISeqEvent.J2_LOADTBL:
                        {
                            var mode = trkInter.rI[0];
                            var destReg = trkInter.rI[1];
                            var tableOffset = trkInter.rI[2];
                            var indexRegister = trkInter.rI[3];
                      
                            if (mode == 0xE)
                            {
                                var reader = trkInter.Sequence;

                                var old_pos = reader.BaseStream.Position;

                                reader.BaseStream.Position = tableOffset + 3 * TrackRegisters[(byte)indexRegister];
                                Console.WriteLine($"[{old_pos:X}]{trackNumber} Reading from 0x{reader.BaseStream.Position:X}");
                                TrackRegisters[(byte)destReg] = (short)Helpers.ReadUInt24BE(trkInter.Sequence);

                                Console.WriteLine($"Loaded address 0x{TrackRegisters[(byte)destReg]:X} ");
                                reader.BaseStream.Position = old_pos;
                            }

                            break;
                        }
                    case JAISeqEvent.J2_JMPTBL:
                        {
                            var addr = trkInter.rI[0];
                            TrackRegisters[0] = (short)addr;
                           

                                //var reader = trkInter.Sequence;
                                //var old_pos = reader.BaseStream.Position;
                                //reader.BaseStream.Position = addr + 3 * 0;//* TrackRegisters[(byte)indexRegister];
                                //Console.WriteLine($"[{old_pos:X}]{trackNumber} Reading from 0x{reader.BaseStream.Position:X}");
                                //var newAddr = (short)Helpers.ReadUInt24BE(trkInter.Sequence);
                                //Console.WriteLine($"{pc:X} Jumping address 0x{addr:X} > ({newAddr:X}) ");
                                //reader.BaseStream.Position = newAddr;
                         

                            break;
                        }
                    case JAISeqEvent.J2_SET_ARTIC:
                        Console.WriteLine($"[{TrackName}@0x{trkInter.pcl:X5}] sets parameter {trkInter.rI[0]:X} to 0x{trkInter.rI[1]:X3} R3 = 0x{TrackRegisters[(byte)trkInter.rI[0]]:X}");
                        if (trkInter.rI[0] == 0x62)
                        {
                            JAISeqPlayer.ppqn = trkInter.rI[1];
                            JAISeqPlayer.recalculateTimebase();
                        }
                        else if (trkInter.rI[0] == 0x64)
                        {
                            trackArticulation = trkInter.rI[1];
                        } else if (trkInter.rI[0] == 0x6E)
                        {
                            Console.WriteLine(trkInter.rI[1]);
                            vibratoDepth = (int)((trkInter.rI[1] / 128f) * 4096f);
                        }
                        else if (trkInter.rI[0] == 0x6F)
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
                        {

             
                                TrackRegisters[3] = (byte)_syncval;
                                TrackRegisters[0] = (byte)_syncval;
                                syncval_written = false;
                                Console.WriteLine($"IMPORTED SYNC {_syncval}");
                           

                            break;
                        }
                    case JAISeqEvent.VIBRATO_PITCH:
                        Console.WriteLine($"Vibpitch {trkInter.rI[0]}");
                        vibPitch = trkInter.rI[0];
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
                            if (snd == null) { error("noteOn", "ADPCM Buffer NULL!", keyNoteVel.wsysid, keyNoteVel.wave); Console.WriteLine(" {4} b{0} p{1} -- n{2} v{3}", bank, program, note, velocity,TrackName); break; }

    
                            var newVoice = new JAIDSPVoice(ref snd);


                            var desiredPitch = (float)Math.Pow(2, ((note - ouData.key) + transpose) / 12f) * currentInst.Pitch * keyNoteVel.Pitch * keyNote.Pitch;
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
                        Console.WriteLine($"Writing port {trkInter.rI[0]}({Ports[trkInter.rI[0]]}) from reg {trkInter.rI[1]}({TrackRegisters[(byte)trkInter.rI[1]]}");
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
                    case JAISeqEvent.NOP:
                        delay = 1;
                        break;
                    case JAISeqEvent.UNKNOWN:

                        var ww = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("E: ");
                        Console.WriteLine("Trk{0} unknown opcode 0x{1:X}({2}) @ {3:X}", trackNumber, trkInter.last_opcode, (JAISeqEvent)trkInter.last_opcode, trkInter.pc);
                        Console.ForegroundColor = ww;
                        //crash();

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
                            var tid = Parent == null ? $"{newTrk.trackNumber}" : $"{this.trackNumber}.{trackID}";
                            newTrk.TrackName = Parent == null ? $"Track {newTrk.trackNumber}" : $"Child {this.trackNumber}.{trackID}";
                            newTrk.muted = getTrackMuteArg(tid);
                            newTrk.Parent = this;
                            Console.WriteLine($"OVERRIDE {trackID} New track at 0x{data:X}");
                            if (Children.ContainsKey(trackID))
                            {
                                Children[trackID].destroy();
                                Children.Remove(trackID);
                            }

                            Children.Add(trackID,newTrk);
                     
                            break;
                        }
                    case JAISeqEvent.J2_OVERRIDE:
                        {
             
                            if (trkInter.rI[1] != 0xC1) { crash(); break; }
                            if (trkInter.rI[0] != 0x40) { crash(); break; }
                            var trackID = trkInter.rI[2];
                            var data = TrackRegisters[(byte)trkInter.rI[3]];
                            var newTrk = new JAISeqTrack(ref bmsData, data, interVer);
                            newTrk.trackNumber = trackID;
                            var tid = Parent == null ? $"{newTrk.trackNumber}" : $"{this.trackNumber}.{trackID}";
                            newTrk.TrackName = Parent == null ? $"Track {newTrk.trackNumber}" : $"Child {this.trackNumber}.{trackID}";
                            newTrk.muted = getTrackMuteArg(tid);
                            Console.WriteLine($"OVERRIDE {newTrk.TrackName} New track at 0x{data:X}");
                            newTrk.Parent = this;


                            if (Children.ContainsKey(trackID))
                            {
                                Children[trackID].destroy();
                                Children.Remove(trackID);
                            }

                            Children.Add(trackID, newTrk);

                            break;
                        }
                    case JAISeqEvent.J2_COMPARE:
                    {
                            var mode = trkInter.rI[0];
                            var destReg = (byte)trkInter.rI[1];
                            var Value = trkInter.rI[2];
                            if (mode == 1)
                            {
                                TrackRegisters[3] =  (TrackRegisters[destReg] += (short)Value);
                                Console.WriteLine($"{pc:X} CMPR {TrackRegisters[destReg]} {destReg} ADD {Value} ");
                            }
                            else if (mode == 3)
                            {
                                TrackRegisters[3] = (short)(TrackRegisters[destReg] - (short)Value);
                                Console.WriteLine($"{pc:X}  CMPR {TrackRegisters[destReg]} ({destReg}) COMPARE {Value} ({TrackRegisters[3]})");
                            }

                            break;
                    }

                    case JAISeqEvent.J2_COMPARE_REG:
                        {
                            var mode = trkInter.rI[0];
                            var destReg = (byte)trkInter.rI[1];
                            var srcReg = (byte)trkInter.rI[2];
                            if (mode == 1)
                            {
                                TrackRegisters[3] = (TrackRegisters[destReg] += TrackRegisters[srcReg]);
                                Console.WriteLine($"{pc:X} CMPRREG {TrackRegisters[destReg]} {destReg} ADD {TrackRegisters[srcReg]} ");
                            }
                            else if (mode == 3)
                            {
                                TrackRegisters[3] = (short)(TrackRegisters[destReg] - (short)TrackRegisters[srcReg]);
                                Console.WriteLine($"{pc:X}  CMPRREG {destReg}={TrackRegisters[destReg]} COMPARE {srcReg}={TrackRegisters[srcReg]}  ({TrackRegisters[3]})");
                            }

                            break;
                        }

                    case JAISeqEvent.CMP8:
                        {
                            var srcReg = (byte)trkInter.rI[0];
                            var destReg = (byte)trkInter.rI[1];

                            TrackRegisters[srcReg] = TrackRegisters[3] = (short)(TrackRegisters[srcReg] - destReg);
                            break;
                        }
                    case JAISeqEvent.ADD8:
                        {
                            var srcReg = (byte)trkInter.rI[0];
                            var destReg = (byte)trkInter.rI[1];

                            TrackRegisters[srcReg] = TrackRegisters[3] = (short)(TrackRegisters[srcReg] + destReg);
                            break;
                        }

                    case JAISeqEvent.ADDR:
                        {
                            var srcReg = (byte)trkInter.rI[0];
                            var destReg = (byte)trkInter.rI[1];
                            // TrackRegisters[destReg]
                            TrackRegisters[3] = (short)(TrackRegisters[srcReg] + destReg );
                            break;
                        }
                    case JAISeqEvent.MUL8:
                        {
                            var srcReg = (byte)trkInter.rI[0];
                            var destReg = (byte)trkInter.rI[1];

                            TrackRegisters[srcReg] = TrackRegisters[3] = (short)(TrackRegisters[srcReg] * destReg);
                            break;
                        }
                    case JAISeqEvent.ADD16:
                        {
                            var srcReg = (byte)trkInter.rI[0];
                            var value = trkInter.rI[1];
                            //Console.Write($"adding {value} Before {TrackRegisters[srcReg]} after={TrackRegisters[srcReg] + value}");
                            TrackRegisters[srcReg] = TrackRegisters[3] = (short)(TrackRegisters[srcReg] + value);
                            break;
                        }
                    case JAISeqEvent.TRANSPOSE:
                        transpose = trkInter.rI[0];
                        break;
                    case JAISeqEvent.WRITE_CHILD_PORT:
                        {
                            var port = trkInter.rI[0];
                            var reg = trkInter.rI[1];

                            var child_id = (port >> 4) & 0xF;
                            var portid = port & 0xF;

                            Console.WriteLine($"Writing r0 = {TrackRegisters[(byte)reg]} to port {portid} on child {child_id:X} {port:X}");

                            if (Children.ContainsKey(child_id))
                                Children[child_id].Ports[portid]  = TrackRegisters[(byte)reg];

                            break;
                        }
                    case JAISeqEvent.J2_WRITE_CHILD:
                        {
                            var port = trkInter.rI[0];
                            var reg = trkInter.rI[1];
                            foreach (KeyValuePair<int,JAISeqTrack> child in Children)
                                child.Value.Ports[port] = TrackRegisters[(byte)reg];

                            break;
                        }
                    case JAISeqEvent.J2_WRITE_PARENT:
                        {

                            var port = trkInter.rI[0];
                            var reg = trkInter.rI[1];
                            if (Parent != null)
                                Parent.Ports[port] = TrackRegisters[(byte)(reg)];
                            

                            break;

                        }
                    case JAISeqEvent.CLOSE_TRACK:
                    case JAISeqEvent.J2_CLOSE_TRACK:
                        {   
                            var trackID = trkInter.rI[0];
                            if (Children.ContainsKey(trackID))
                            {
                                Children[trackID].destroy();
                                Children.Remove(trackID);
                            }
                            break;
                        }


                    case JAISeqEvent.WAIT_REGISTER:
                        //crash();
                        delay += 0xFF;
                        break;
                    case JAISeqEvent.MISS:
                        Console.WriteLine($"BAD OPCODE {lastOpcode}");
                        crash();
                        break;
                    case JAISeqEvent.INTERRUPT:
                        if (this==JAISeqPlayer.RootTrack)
                            interrupt_pause = true;
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
