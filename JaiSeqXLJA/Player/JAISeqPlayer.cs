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
        public static int ppqn = 100;
        public static int bpm = 120;

        public static JAISeqTrack[] tracks = new JAISeqTrack[64];

        private static Stopwatch tickTimer;
        private static float tickLength;
        private static int ticks = 0;
        public static float gainMultiplier = 0.3f;
        public static bool paused = false;

        public static float timebaseValue
        {
            get { return tickLength; }
        }

        private static Dictionary<int, JAIDSPSoundBuffer> waveCache;
        private static Dictionary<string, Stream> awHandles;
        public static JASystem JASPtr;

        public static void startPlayback(string file, ref JASystem sys, JAISeqInterpreterVersion seqVer)
        {
            Console.WriteLine($"Engine statrting with intver {seqVer}");
            var contents = File.ReadAllBytes(file);
            tracks[0] = new JAISeqTrack(ref contents,0x00,seqVer); // entry point.
            tracks[0].trackNumber = -1;
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
                            if (jwg.awFile.Length > 2)
                            {
                                awHandles[jwg.awFile] = File.OpenRead("Banks/" + jwg.awFile);
                            } else
                            {
                                Console.WriteLine("Ignoring WSYS (name too short)");
                            }
                        }
                    }
                }
            }
            recalculateTimebase();
        }

        public static void setTrackMuted(int trkid, bool muted)
        {
            for (int trk = 0; trk < tracks.Length; trk++)
            {
                if (tracks[trk] != null && tracks[trk].trackNumber == trkid)
                {
                    tracks[trk].muted = muted;
                    Console.WriteLine($"Track {trk} mute: {tracks[trk].muted} ({trkid})");
                    if (tracks[trk].muted)
                        tracks[trk].purgeVoices();
                    break;
                }
            }
        }

        public static void cycleTrackMuted(int trkid)
        {

            for (int trk = 0; trk < tracks.Length; trk++)
            { 
                if (tracks[trk] != null && tracks[trk].trackNumber==trkid)
                {
                    tracks[trk].muted = !tracks[trk].muted;
                    Console.WriteLine($"Track {trk} mute: {tracks[trk].muted} ({trkid})");
                    if (tracks[trk].muted)
                        tracks[trk].purgeVoices();
                    break;
                }
            }
        }

        /* Ugh god, this turned out more awful than i wanted it to. Redo this in the future. This currently has massive perf implications */
        public static JAIDSPSoundBuffer loadSound(int wsys_id, int waveID, out JWave data)
        {
            var cacheIndex = (wsys_id << 16) | waveID;
            data = null;
            JAIDSPSoundBuffer ret;
            // Not in cache.
            var CWsys = JASPtr.WaveBanks[wsys_id]; // Check for WSYS existence
            if (CWsys==null)
            {
                Console.WriteLine("Null WSYS request id:{0} wavid:{1}");
                return null;
            }
            JWave waveData;
            var ok = CWsys.WaveTable.TryGetValue(waveID, out waveData); // Check for the WAVEID inside of the WSYS
            if (!ok)
            {
                var w = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("[JAIDSP]: ");
                Console.ForegroundColor = w;
                Console.WriteLine("WSYS doesn't contain wave id:{0} wavid:{1}", wsys_id,waveID);
           
                return null;
            }
            ok = waveCache.TryGetValue(cacheIndex, out ret); // check if it is in cache
            if (ok)
            {
                data = waveData;
                return ret; // return the preloaded data if in cache
            }
            Stream fhnd;
            var ok2 = awHandles.TryGetValue(waveData.wsysFile, out fhnd); // Check to see if the AWHandle exists 
            if (!ok2)
            {
                try
                {
                    var w = File.OpenRead("Banks/" + waveData.wsysFile); // 
                    awHandles[waveData.wsysFile] = w;
                    var fg = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("EMERGENCY: ");
                    Console.ForegroundColor = fg;
                    Console.WriteLine("Handle for {0} missing. Attempting hot load...", waveData.wsysFile);

                    fhnd = w;
                } catch
                {
                    Console.WriteLine("FAILURE.");
                    return null;
                }
            }

            fhnd.Position = waveData.wsys_start;
            byte[] ou = new byte[waveData.wsys_size];
            fhnd.Read(ou, 0, waveData.wsys_size);
            var pcm = ADPCM.ADPCMToPCM16(ou, (ADPCM.ADPCMFormat)waveData.format);
            
            if (!Directory.Exists("wavout"))
                Directory.CreateDirectory("wavout");
            

           
            JAIDSPSoundBuffer sbuf; 
            if (waveData.loop)
                sbuf = JAIDSP.SetupSoundBuffer(pcm, 1, (int)waveData.sampleRate, 16, waveData.loop_start, waveData.loop_end);
            else
                 sbuf = JAIDSP.SetupSoundBuffer(pcm, 1, (int)waveData.sampleRate, 16);
            
            //File.WriteAllBytes($"wavout/{wsys_id}_{waveID}.wav",sbuf.generateFileBuffer());
           // if (waveData.loop)
                //File.WriteAllText($"wavout/{wsys_id}_{waveID}_loop.txt", $"{waveData.loop_start},{waveData.loop_end}");
            

            waveCache[cacheIndex] = sbuf;
            data = waveData;
            return sbuf;
        }


        public static void recalculateTimebase()
        {
            tickLength = (60000f / (float)(bpm)) / ((float)ppqn);
            ticks = (int)(tickTimer.ElapsedMilliseconds / tickLength);
            Console.WriteLine("Timebase updated {0}bpm {1}ppqn cycle-length {2} @ {3}", bpm, ppqn, tickLength, ticks);
        }

        public static void update()
        {
            var ts = tickTimer.ElapsedMilliseconds;
            var tt_n = ts / tickLength;
            while (ticks < tt_n)
                try
                {
                    tt_n = ts / tickLength;
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
        public static void tick()
         {
            ticks++;
            for (int i=0; i < tracks.Length; i++)
                if (tracks[i]!=null)
                    if (!paused)
                        tracks[i].update();
        }

        public static void addTrack(int id, JAISeqTrack trk)
        {
            if (tracks[id + 1]!=null)
                tracks[id + 1].destroy();
            tracks[id + 1] = trk;
        }

    }
}
