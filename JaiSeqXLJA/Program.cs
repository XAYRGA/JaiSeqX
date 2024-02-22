#define heaptagging


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using libJAudio;
using JaiSeqXLJA.DSP;
using System.Threading.Tasks;
using System.Threading;
using libJAudio.Sequence.Inter;
using JaiSeqXLJA.Player;
using JaiSeqXLJA.Visualizer;
using xayrga.JAIDSP;

namespace JaiSeqXLJA
{
    public class JaiSeqXLJA
    {
        public static string[] cmdargs;
        public static JASystem JASystem;


        static void Main(string[] args)
        {
            
#if DEBUG 
            args = new string[]
            {
                @"jaiinit_sms.aaf",
                "visu",
                "t_boss.com.bms",
                "0",
                "-paused",
                "-jdsp.device",
                "1"

            };
#endif  

      
           // Console.ReadLine();
            
            Console.WriteLine("Initializing DSP.");

            cmdargs = args; // push args into global table.
            var jaiiInitFile = assertArg(0, "JAIInitFile");
            var taskFunction = assertArg(1, "Task");


            if (!File.Exists(jaiiInitFile))
                assert("Cannot find JAIInitFile {0}", jaiiInitFile);
            var jaiinitf = File.ReadAllBytes(jaiiInitFile); // read entire JAIInitFile
            JASystem = libJAudio.Loaders.JASystemLoader.loadJASystem(ref jaiinitf); // Load the JASystem (will automatically be detected by JAIInitVersionDetector)

            switch (taskFunction)
            {
                case "extractwsys":
                    {
                         
                        break;
                    }

                case "visu":
                case "play":
                    {
                        var sequenceFile = assertArg(2, "SequenceFile");
                        var sequenceVersion = assertArgNum(3, "SequenceVersion");

                        if (!File.Exists(sequenceFile))
                            assert("Cannot find SequenceFile {0}", sequenceFile);
                        JAIDSP.Init(); // bpth play and visu require the sound engine.       
                        Player.JAISeqPlayer.init();
                        Player.JAISeqPlayer.noDKJBWhistle = findDynamicFlagArgument("-nodkwhistle");
                        Player.JAISeqPlayer.startPlayback(sequenceFile, ref JASystem, (JAISeqInterpreterVersion)sequenceVersion);
                        var useVisu = taskFunction == "visu";
                        if (useVisu)
                            Menu.init();

                       

                        while (true)
                        {
                            Player.JAISeqPlayer.update();
                            Thread.Sleep(1);
                            if (useVisu)
                                Menu.update();
                        }
                        break;
                    }
            }
            Console.ReadLine();
            //*/
            
        }

        public static string assertArg(int argn, string assert)
        {
            if (cmdargs.Length <= argn)
            {
                Console.WriteLine("Missing required argument #{0} for '{1}'", argn, assert);
                Environment.Exit(0);
            }
            return cmdargs[argn];
        }

        public static string findDynamicStringArgument(string name, string def)
        {
            for (int i = 0; i < cmdargs.Length; i++)
            {
                if (cmdargs[i] == name || cmdargs[i] == "-" + name)
                {
                    if (cmdargs.Length >= i + 1)
                        return cmdargs[i + 1];
                    break;
                }
            }
            return def;
        }

        public static int findDynamicNumberArgument(string name, int def)
        {
            for (int i = 0; i < cmdargs.Length; i++)
            {
  
                if (cmdargs[i] == name)
                {
         
                        int v = 0;
                        var ok = Int32.TryParse(cmdargs[i + 1], out v);
                        if (!ok)
                        {
                            Console.WriteLine($"Invalid parameter for '{cmdargs[i]}' (Number expected, couldn't parse '{cmdargs[i + 1]}' as a number.)");
                            Environment.Exit(0);
                        }
                    return v;
                }
            }
            return def;
        }

        public static bool findDynamicFlagArgument(string name)
        {
            for (int i = 0; i < cmdargs.Length; i++)
            {
                if (cmdargs[i] == name)
                {
                    return true;
                }
            }
            return false;
        }

        public static int assertArgNum(int argn, string assert)
        {
            if (cmdargs.Length <= argn)
            {
                Console.WriteLine("Missing required argument #{0} for '{1}'", argn, assert);
                Environment.Exit(0);
            }
            int b = 1;
            var w = Int32.TryParse(cmdargs[argn], out b);
            if (w == false)
            {
                Console.WriteLine("Cannot parse argument #{0} for '{1}' (expected number, got {2}) ", argn, assert, cmdargs[argn]);
                Environment.Exit(0);
            }
            return b;
        }

        public static string tryArg(int argn, string assert)
        {
            if (cmdargs.Length <= argn)
            {
                if (assert != null)
                {
                    Console.WriteLine("No argument #{0} specified {1}.", argn, assert);
                }
                return null;
            }
            return cmdargs[argn];
        }
        public static void assert(string text, params object[] wtf)
        {
            Console.WriteLine(text, wtf);
            Environment.Exit(0);
        }
    }
}
