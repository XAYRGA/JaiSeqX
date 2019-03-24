using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JaiSeqX.JAI.Seq;
using JaiSeqX.JAI;
using System.IO;
using System.Threading;
using XAYRGA.SharpSL;

namespace JaiSeqX.Player
{
    public static class BMSPlayer
    {
        static byte[] BMSData; // byte data of our music
        public static int bpm; // beats per minute
        public static int ppqn; // pulses per quarter note
        static int ticklen; // how long it takes before the thread continues
        static Subroutine[] subroutines; // Container for our subroutines
        static Thread playbackThread; // read name
        static bool[] halts; // Halted tracks
        static bool[] mutes;  // Muted tracks
        static int subroutine_count; // Internal counter for track ID (for creating new tracks)
        static BMSChannelManager ChannelManager;
        static AABase AAF;

        public static void LoadBMS(string file, ref AABase AudioData)
        {
            AAF = AudioData; // copy our data into here. 
            BMSData = File.ReadAllBytes(file); // Read the BMS file
            subroutines = new Subroutine[32]; // Initialize subroutine array. 
            halts = new bool[32];
            mutes = new bool[32];


            ChannelManager = new BMSChannelManager();
            bpm = 1000; // Dummy value, should be set by the root track
            ppqn = 1; // Dummmy, ^ 
            updateTempo(); // Generate the trick length, also dummy.
            // Initialize first track.
            var root = new Subroutine(ref BMSData, 0x000000); // Should always start at 0x000 of our data.
            subroutine_count = 1; 
            subroutines[0] = root; // stuff it into the subroutines array. 

            playbackThread = new Thread(new ThreadStart(doPlayback));
            playbackThread.Start(); // go go go
        }

       
        private static void updateTempo()
        {
            try {
                ticklen = (60000 / (bpm)) / (ppqn);    // lots of divison :D        
            } catch
            {
                // uuuuUUGH. ZERO. 
            }
        }

        private static void doPlayback()
        {
            while (true)
            {
                try
                {
                    sequencerTick(); // run the sequencer tick. 
                                     // Just going to leave this for timing.
                    Thread.Sleep(ticklen); // sleeeeep 
                } catch (Exception E)
                {
                    Console.WriteLine("SEQUENCER MISSED TICK");
                    Console.WriteLine(E.ToString());
                }
            }
        }

        private static void sequencerTick()
        {
            ChannelManager.onTick();
            for (int csub = 0; csub < subroutine_count; csub++)
            {
                var current_subroutine = subroutines[csub]; // grab the current subroutine
                var current_state = current_subroutine.State; // Just for helper
                while (current_state.delay < 1) // we want to go until there's a delay. A delay counts as a BREAK command, all other commands are executed inline. 
                {
                    var opcode = current_subroutine.loadNextOp(); // loads the next opcode
                    /* State machine for sequencer */ 

                    switch (opcode)
                    {
                        case JaiEventType.TIME_BASE:
                            bpm = current_state.bpm;
                            ppqn = current_state.ppqn;
                            updateTempo();
                            break;
                        case JaiEventType.NOTE_ON:
                            {
                                var bankdata = AAF.IBNK[current_state.voice_bank];
                                if (bankdata!=null)
                                {
                                    var program = bankdata.Instruments[current_state.voice_program];
                                    if (program!=null)
                                    {
                                        var note = current_state.note;
                                        var vel = current_state.vel;
                                        //Console.WriteLine("{2}: {0} {1}", note, vel,csub);
                                        var notedata = program.Keys[note]; // these are interpolated, no need for checks.
                                        var key = notedata.keys[vel]; // These too. 
                                        // Basically, if everything is valid up to this point, we should be good. (should, at least for the IBNK)
                                        if (key!=null)
                                        {
                                            try
                                            {
                                                var wsysid = key.wsysid;
                                                var waveid = key.wave;
                                                var wsys = AAF.WSYS[wsysid];
                                                if (wsys != null)
                                                {
                                                    var wave = wsys.waves[waveid];
                                                    var sound = ChannelManager.loadSound(wave.pcmpath,wave.loop,wave.loop_start,wave.loop_end).CreateInstance();
                                                    var pmul = program.Pitch * key.Pitch;
                                                    var vmul = program.Volume * key.Volume;
                                                    var real_pitch = Math.Pow(2, ((note - wave.key) *pmul ) / 12) ;
                                                    var true_volume = (Math.Pow(((float)vel) / 127, 2) * vmul) * 0.5;
                                                    sound.Volume = (float)(true_volume * 0.6);
                                                    
                                                    if (program.IsPercussion)
                                                    {
                                                        real_pitch = (float)(key.Pitch * program.Pitch);
                                                    }
                                                    sound.Pitch = (float) (real_pitch);

                                                    ChannelManager.startVoice(sound, (byte)csub, current_state.voice);
                                                    if (!mutes[csub]) // The sounds are created, so they're still startable even if they're not used. 
                                                    {
                                                        sound.Play();
                                                    }

                                                } else {
                                                    Console.WriteLine("Null WSYS??");
                                                }
                                            }catch (Exception E)
                                            {
                                                var b = Console.ForegroundColor;
                                                Console.ForegroundColor = ConsoleColor.Red;
                                                Console.WriteLine("fuuuuuck");
                                                Console.WriteLine(E.ToString());
                                                Console.ForegroundColor = b;
                                            }

                                            
                                        }
                                    } else
                                    {
                                        Console.WriteLine("Null IBNK Program");
                                    }
                                } else
                                {
                                    Console.WriteLine("Null bank data {0}", current_state.voice_bank);
                                }
                            }
                            break;
                        case JaiEventType.NOTE_OFF:
                            {
                                ChannelManager.stopVoice((byte)csub, current_state.voice);
                                break;
                            }
                        case JaiEventType.NEW_TRACK:
                            {
                                var ns = new Subroutine(ref BMSData, current_state.track_address);
                                subroutines[subroutine_count] = ns;
                                subroutine_count++;
                                break;
                            }
                        case JaiEventType.HALT:
                            {
                                halts[csub] = true;
                                break;
                            }
                        case JaiEventType.PERF:
                            {
                                if (current_state.perf==1) // Pitch bend
                                {
                                    //Console.WriteLine("Pitch bend c {0} {1} {2}", csub, current_state.perf_value, current_state.perf_duration);
                                    ChannelManager.doPitchBend((byte)csub, current_state.perf_value, current_state.perf_duration, current_state.perf_type);
                                }
                                break;
                            }
                        case JaiEventType.JUMP:
                            {
                                Console.WriteLine("Track {0} jumps to 0x{1:X}", csub, current_state.jump_address);
                                current_subroutine.jump(current_state.jump_address);
                                break;
                            }
                        case JaiEventType.DELAY: // handled internally. 
                            
                            break;
                        case JaiEventType.UNKNOWN_ALIGN_FAIL:
                            Console.WriteLine("==== Sequence Crash ====");
                            Console.WriteLine("Track Number: {0}", csub);
                            Console.WriteLine("Stack: \n");
                            Helpers.printJaiSeqStack(current_subroutine);
                            while (true) { Console.ReadLine(); }
                            break;
                    }

                }
                if (current_state.delay > 0) { // check if the delay is over 0
                    current_state.delay--; // if it is, this executes every tick, so subtract one tick from it. 
                }
            }
        }
    }
}
