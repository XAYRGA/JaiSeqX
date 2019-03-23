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
        static AABase AAF;

        public static void LoadBMS(string file, ref AABase AudioData)
        {
            AAF = AudioData; // copy our data into here. 
            BMSData = File.ReadAllBytes(file); // Read the BMS file
            subroutines = new Subroutine[32]; // Initialize subroutine array. 
            halts = new bool[32];
            mutes = new bool[32];

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
                sequencerTick(); // run the sequencer tick. 
                // Just going to leave this for timing.
                Thread.Sleep(ticklen); // sleeeeep 
            }
        }

        private static void sequencerTick()
        {
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
                            
                            break;
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
