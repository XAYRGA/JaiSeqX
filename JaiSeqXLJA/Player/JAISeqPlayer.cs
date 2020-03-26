using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libJAudio.Sequence;
using libJAudio.Sequence.Inter;
using JaiSeqXLJA.DSP;
using libJAudio;
using System.IO;
using System.Diagnostics;


namespace JaiSeqXLJA.Player
{
    public static class JAISeqPlayer
    {
        public static int ppqn = 3000;
        public static int bpm = 120;

        public static JAISeqTrack[] tracks = new JAISeqTrack[64];

        private static Stopwatch tickTimer;
        private static float tickLength;
        private static int ticks = 0;

        private static Dictionary<int, JAIDSPSoundBuffer> waveCache;
        private static Dictionary<string, Stream> awHandles;

        public static JASystem JASPtr;


        public static void startPlayback(string file, ref JASystem sys)
        {
            var contents = File.ReadAllBytes(file);
            tracks[0] = new JAISeqTrack(ref contents,0x0000000); // entry point.
            tickTimer = new Stopwatch();
            tickTimer.Start();
            JASPtr = sys;
            waveCache = new Dictionary<int, JAIDSPSoundBuffer>();
            awHandles = new Dictionary<string, Stream>();
            var wsc = sys.WaveBanks.Count();
            // Create AW Handles 
            foreach ( JWaveSystem jws in sys.WaveBanks)
            {
                if (jws!=null)
                {
                    foreach (JWaveGroup jwg in jws.Groups)
                    {
                        if (jwg != null)
                        {
                            Console.WriteLine("Creating handle for {0}", jwg.awFile);
                            awHandles[jwg.awFile] = File.OpenRead("Banks/" + jwg.awFile);
                        }
                    }
                }
            }
            recalculateTimebase();
        }

        /* Ugh god, this turned out more awful than i wanted it to. Redo this in the future. This currently has massive perf implications */
        public static JAIDSPSoundBuffer loadSound(int wsys_id, int waveID, out JWave data)
        {
            var cacheIndex = (wsys_id << 16) | waveID;
            data = null;
            JAIDSPSoundBuffer ret;
            // Not in cache.
            var CWsys = JASPtr.WaveBanks[wsys_id];
            if (CWsys==null)
            {
                Console.WriteLine("Null WSYS request id:{0} wavid:{1}");
                return null;
            }
            JWave waveData;
            var ok = CWsys.WaveTable.TryGetValue(waveID, out waveData);
            if (!ok)
            {
                Console.WriteLine("WSYS doesn't contain wave id:{0} wavid:{1}");
                return null;
            }
            ok = waveCache.TryGetValue(cacheIndex, out ret);
            if (ok)
            {
                data = waveData;
                return ret;
            }
            var fhnd = awHandles[waveData.wsysFile];
            fhnd.Position = waveData.wsys_start;
            byte[] ou = new byte[waveData.wsys_size];
            fhnd.Read(ou, 0, waveData.wsys_size);
            var pcm = ADPCM.ADPCMToPCM16(ou, (ADPCM.ADPCMFormat)waveData.format);
            JAIDSPSoundBuffer sbuf; 
            if (waveData.loop)
            {
                sbuf = JAIDSP.SetupSoundBuffer(pcm, 1, (int)waveData.sampleRate, 16, waveData.loop_start, waveData.loop_end);
            } else
            {
                sbuf = JAIDSP.SetupSoundBuffer(pcm, 1, (int)waveData.sampleRate, 16);
            }

            waveCache[cacheIndex] = sbuf;
            data = waveData;
            return sbuf;
        }

       

        
        
        public static void recalculateTimebase()
        {
            tickLength = (60000f / (float)(bpm)) / ((float)ppqn);
            ticks = (int)(tickTimer.ElapsedMilliseconds / tickLength);
            Console.WriteLine(tickLength);
        }

        public static void update()
        {
            var ts = tickTimer.ElapsedMilliseconds;
            var tt_n = ts / tickLength;
            while (ticks < tt_n)
            {
                try
                {
                    tick();
            
                }
                catch (Exception E)
                {
                    var w = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("SEQUENCER MISSED TICK");
                    Console.WriteLine(E.ToString());
                    Console.ForegroundColor = w;
                }
            }
        }
        public static void tick()
         {
            ticks++;
            for (int i=0; i < tracks.Length; i++)
            {
                if (tracks[i]!=null)
                {

                    tracks[i].update();
                    //Console.Write("T{0}: {1}|", i, tracks[i].ticks);
                }
            }
            //Console.WriteLine();
        }

        public static void addTrack(int id, JAISeqTrack trk)
        {
            if (tracks[id]!=null)
            {
                tracks[id].destroy();
            }
            tracks[id] = trk;
        }

    }
}
