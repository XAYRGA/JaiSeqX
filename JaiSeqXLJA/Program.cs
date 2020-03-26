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

            var jaiinit = File.ReadAllBytes("JAIInit.aaf"); // read entire JAIInitFile
            JASystem = libJAudio.Loaders.JASystemLoader.loadJASystem(ref jaiinit); // Load the JASystem (will automatically be detected by JAIInitVersionDetector)
            Console.WriteLine("Loaded JASystem");
            /*
            foreach (JWaveSystem w in System.WaveBanks)
            {
                if (w != null)
                {
                    foreach (JWaveGroup wg in w.Groups)
                    {
                        var waveinst = 0;
                        Console.Write("{0} Transforming wavegroup ....", wg.awFile);
                        if (wg!=null)
                        {

                            waveinst++;
                            Console.Write("{0}, ", waveinst);
                            if (!Directory.Exists("WSYS_CACHE/" + wg.awFile))
                                Directory.CreateDirectory("WSYS_CACHE/" + wg.awFile);
                            var awhnd = File.OpenRead("Banks/" + wg.awFile);
                            foreach (JWave wv in wg.Waves)
                            {
                                if (wv!=null)
                                {
                                    awhnd.Position = wv.wsys_start;
                                    byte[] dat = new byte[wv.wsys_size];
                                    awhnd.Read(dat, 0, wv.wsys_size);
                                    byte[] pcm = ADPCM.ADPCMToPCM16(dat, ADPCM.ADPCMFormat.FOUR_BIT);
                                    /*
                                    Console.WriteLine("{0} {1} ", wv.sampleRate, (int)wv.sampleRate);
                                    int cn = 0;
                                    int sr = 0;
                                    int br = 0;
                                    byte[] pcm2 = WAV.LoadWAVFromFile(@"C:\Users\Dane\source\repos\JaiSeqX\bin\Debug\WSYS_CACHE\AW_LuiSec0_0.aw\0x0.wav", out cn, out br, out sr);
                                    Console.WriteLine("{0} {1} {2}",cn, sr, br);
                                    Console.WriteLine("{0}", wv.sampleRate);
                          

                                    var sbuf = JAIDSP.SetupSoundBuffer(pcm, 1, (int)wv.sampleRate, 16);
                                    using (JAIDSPVoice voi = new JAIDSPVoice(ref sbuf))
                                    {
                                        voi.play();
                                        voi.setEffectParams(VoiceEffect.REVERB, 0.5F, 0.5F);
                                        Console.ReadLine();
                                    }
                               
                                }                            
                            }
                        }
                    }
                    Console.WriteLine("OK.");

                }            
            }
            */
            // 6/28
            /*
            var inst = JASystem.Banks[6].Instruments[42];
            var waveGroup = JASystem.WaveBanks[inst.Keys[64].Velocities[127].wsysid].Groups[0];
            var wave = waveGroup.WaveByID[inst.Keys[64].Velocities[127].wave];
            var awhnd = File.OpenRead("Banks/" + waveGroup.awFile);
            awhnd.Position = wave.wsys_start;
            byte[] dat = new byte[wave.wsys_size];
            awhnd.Read(dat, 0, wave.wsys_size);
            byte[] pcm = ADPCM.ADPCMToPCM16(dat, ADPCM.ADPCMFormat.FOUR_BIT);
            var sbuf = JAIDSP.SetupSoundBuffer(pcm, 1, (int)wave.sampleRate, 16, (int)Math.Floor((float)(wave.loop_start)), (int)Math.Floor((float)(wave.loop_end )));
            JAIDSPVoice voi = new JAIDSPVoice(ref sbuf);
            voi.setOcillator(inst.oscillators[0]);
            //voi.setEffectParams(VoiceEffect.REVERB, 0.5f, 0.5f);
            voi.play();
            voi.stop();
            */
            Player.JAISeqPlayer.startPlayback("TelesaBattle.bms", ref JASystem);

            while (true)
            {
                Player.JAISeqPlayer.update();
                Thread.Sleep(2);
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
