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

namespace JaiSeqXLJA
{
    class JaiSeqXLJA
    {
        public static string[] cmdargs;
        public static JASystem JASystem;

        static void Main(string[] args)
        {

            args = new string[]
            {
                "donkey.baa",
                "play",
                "dk/bind00.bms",
                "1",
            };
            /*
            Console.WriteLine("Initializing DSP.");
            JAIDSP.Init();
            Console.ReadLine();
            Console.WriteLine("Initializing JASystem.");
            var jaiinit = File.ReadAllBytes("jaiinit.aaf"); // read entire JAIInitFile
            JASystem = libJAudio.Loaders.JASystemLoader.loadJASystem(ref jaiinit); // Load the JASystem (will automatically be detected by JAIInitVersionDetector)
            Console.WriteLine("Loaded JASystem");
   
            /*
            foreach (JIBank bnk in JASystem.Banks)
            {
                if (bnk == null)
                    continue;
                var ii = 0;
                foreach(JInstrument inst in bnk.Instruments)
                {
                    ii++;
                    if (inst == null)
                        continue;
                    if (inst.oscillators == null)
                        continue;
                    var osc1 = inst.oscillators[0];

                    var env = osc1.envelopes[0];
                    //var FO = File.OpenWrite($"envOut/{bnk.id}_{inst.id}_0.csv");
                    var SB = new StringBuilder();
                    SB.Append("MODE,TIME,VALUE\r\n");

                    JEnvelopeVector vec = env.vectorList[0];
                    while (vec.next!=null)
                    {
                        SB.Append($"{vec.mode},{vec.time},{vec.value}\r\n");
                        vec = vec.next;
                    }
                    File.WriteAllText($"envOut/{bnk.id}_{ii}_0.csv", SB.ToString());



                }
            }

            if (true)
                return;
      */
           // Player.JAISeqPlayer.startPlayback("moonsetter.bms", ref JASystem, libJAudio.Sequence.Inter.JAISeqInterpreterVersion.JA1);



   

            cmdargs = args; // push args into global table.
            var jaiiInitFile = assertArg(0, "JAIInitFile");
            var taskFunction = assertArg(1, "Task");
            //*//*//*//*
            // Load JAIInitFile
            // Load JASystem
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
                        JAIDSP.Init(); // bpth play and visu require the sound engine.       
                        Player.JAISeqPlayer.startPlayback(sequenceFile, ref JASystem, (JAISeqInterpreterVersion)sequenceVersion);
                        Menu.init();
                        while (true)
                        {
                            Player.JAISeqPlayer.update();
                            Thread.Sleep(1);
                            if (Console.KeyAvailable)
                            {
                                var w = Console.ReadKey(true);
                               JAISeqPlayer.cycleTrackMuted((int)w.Key - 64);

                            }
                            Menu.update();
                        }
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

        public static string findDynamicStringArgument(string name, string def)
        {
            for (int i = 0; i < cmdargs.Length; i++)
            {
                if (cmdargs[i]==name) // THANK YOU FOR STOPPING BY
                {
                    if (cmdargs.Length < i + 1) // I REALLY REALLY REALLY LIKE THIS IMAGE
                    {
                        return cmdargs[i + 1]; // (I like it too)
                    }
                    break; // haha great pic
                }
            }
            return def; // thanks lori.
        }


        public static int findDynamicNumberArgument(string name, int def)
        {
            for (int i = 0; i < cmdargs.Length; i++)
            {
                if (cmdargs[i] == name)
                {
                    if (cmdargs.Length < i + 1)
                    {
                        return Convert.ToInt32(cmdargs[i + 1]);
                    }
                    break;
                }
            }
            return def;
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
