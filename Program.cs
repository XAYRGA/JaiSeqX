using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JaiSeqX.JAI;
using JaiSeqX.JAI.Seq;
using System.IO;
using System.Drawing;
using System.Threading.Tasks;

namespace JaiSeqX
{
    public static class JaiSeqX
    {
        public static AABase AAData;

        static void Main(string[] args)
        {

           
        
#if DEBUG
            args = new string[2];
            args[0] = "JaiInit.aaf";
            args[1] = "0"; 
#endif
            JAIVersion SequencerVersion = JAIVersion.UNKNOWN; 
           if (args.Length < 2)
            {
                Console.WriteLine("You need to specify JAIInitFile and JAIVersion");
                Environment.Exit(-1);
            }

           try
            {
                SequencerVersion = (JAIVersion)(Convert.ToUInt16(args[1]));
            } catch
            {
                Console.WriteLine("{0} is not a valid JAIVersion", args[1]);
            }
            if (SequencerVersion==JAIVersion.UNKNOWN)
            {
                Console.WriteLine("{0} is not a valid JAIVersion", args[1]);
            }

            if (SequencerVersion==JAIVersion.ONE)
            {
                var b = new AAFFile();
                b.LoadAAFile(args[0],JAIVersion.ONE);
                AAData = b;
            } else
            {
                var b = new BAAFile();
                b.LoadBAAFile(args[0], SequencerVersion);
                AAData = b;
            }
            // */

            Player.BMSPlayer.LoadBMS("test.bms", ref AAData);

            /* 
             
            var bmsfile = File.ReadAllBytes("test.bms");
            var  first = new Subroutine(ref bmsfile,0x00000);
            var subroutines = new Subroutine[24];
            var subrotid = 1;
            var bpm = 10000;
            var ppqn = 1;
            var tick_length = (60000 / ((float)bpm)) / (ppqn);
            var halt = new bool[24];


            subroutines[0] = first;



            while (true)
            {
                for (int subr = 0; subr < subrotid;subr++)
                {
                    var current_rot = subroutines[subr];
                    var state = current_rot.State;


                    while (state.delay < 1) 
                    {

                        var next_op = current_rot.loadNextOp();

                       // state = current_rot.State;
                        Console.Write("TRK: {0}", subr);
                        Console.WriteLine(next_op);
                        switch (next_op)
                        {
                            case JaiEventType.TIME_BASE:
                                tick_length = (60000 / ((float)state.bpm)) / (state.ppqn);
                                break;
                            case JaiEventType.NEW_TRACK:
                                {
                                    
                                    var ns = new Subroutine(ref bmsfile, state.track_address);
                                    subroutines[subrotid] = ns;
                                    subrotid++;
                                    break;

                                }
                            case JaiEventType.HALT:
                                halt[subr] = true;
                                break;

                            case JaiEventType.JUMP:
                                {
                                    current_rot.jump(state.jump_address);
                                    break;
                                }
                            case JaiEventType.DELAY:
                                Console.WriteLine("DELAY NEW {0}", state.delay);
                                break;
                            case JaiEventType.UNKNOWN_ALIGN_FAIL:
                                Console.WriteLine("==== Sequence Crash ====");
                                Console.WriteLine("Track Number: {0}",subr);
                                Console.WriteLine("Stack: \n");
                                Helpers.printJaiSeqStack(current_rot);
                                while (true) { Console.ReadLine(); }
                                break;
                        }

                        var b = Console.ReadKey();
                        if (b.Key == ConsoleKey.S) 
                        {
                            Helpers.printJaiSeqStack(current_rot);
                        }

                    }
                               
                    
                }
            }

        */ 



            
        }
    }
}
