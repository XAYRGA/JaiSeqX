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


namespace JaiSeqXLJA
{
    class JaiSeqXLJA
    {
        public static string[] cmdargs;
        public static JASystem JASystem;

        static void Main(string[] args)
        {

            JAIDSP.Init();

            var jaiinit = File.ReadAllBytes("twipri/z2sound.baa"); // read entire JAIInitFile
            JASystem = libJAudio.Loaders.JASystemLoader.loadJASystem(ref jaiinit); // Load the JASystem (will automatically be detected by JAIInitVersionDetector)
            Console.WriteLine("Loaded JASystem");
            Player.JAISeqPlayer.startPlayback("twipri/seqs/howl_iyashi_duo.bms", ref JASystem);



            while (true)
            {
                Player.JAISeqPlayer.update();
                Thread.Sleep(1);
            }

            if (true)
                return;
            cmdargs = args; // push args into global table.
            var jaiiInitFile = assertArg(0, "JAIInitFile");
            var taskFunction = assertArg(1, "Task");
            //*//*//*//*
            // Load JAIInitFile
            // Load JASystem
            if (!File.Exists(jaiiInitFile))
                assert("Cannot find JAIInitFile {0}", jaiiInitFile);
            var jaiinitf = File.ReadAllBytes(jaiiInitFile); // read entire JAIInitFile
            JASystem = libJAudio.Loaders.JASystemLoader.loadJASystem(ref jaiinit); // Load the JASystem (will automatically be detected by JAIInitVersionDetector)

            switch (taskFunction)
            {
                case "extractwsys":
                    {
                         
                        break;
                    }

                case "visu":
                case "play":
                    {
                        JAIDSP.Init(); // bpth play and visu require the sound engine.       
                        
                        break;
                    }
            }
        }

        public static string assertArg(int argn,string assert)
        {
            if (cmdargs.Length <= argn )
            {
                
                Console.WriteLine("Missing required argument #{0} for '{1}'", argn, assert); 
                Environment.Exit(0);
            }
            return cmdargs[argn];
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
            if (w==false)
            {
                Console.WriteLine("Cannot parse argument #{0} for '{1}' (expected number, got {2}) ", argn, assert ,cmdargs[argn]);
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
        public static void assert(string text,params object[] wtf)
        {
            Console.WriteLine(text, wtf);
            Environment.Exit(0);
        }
    }
}
