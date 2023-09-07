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
        public static float RuntimeMS;
        private static int ticks = 0;
        public static float gainMultiplier = 1f;
        public static bool paused = false;
        

        public static void init()
        {
            paused = JaiSeqXLJA.findDynamicFlagArgument("-paused");
        }

        public static float timebaseValue
        {
            get { return tickLength; }
        }

        private static Dictionary<int, JAIDSPSampleBuffer> waveCache;
        private static Dictionary<string, Stream> awHandles;
        public static JASystem JASPtr;

        public static void startPlayback(string file, ref JASystem sys, JAISeqInterpreterVersion seqVer)
        {
            Console.WriteLine($"Engine statrting with intver {seqVer}");
            var contents = File.ReadAllBytes(file);
            tracks[0] = new JAISeqTrack(ref contents, 0x000, seqVer); // entry point.
            tracks[0].trackNumber = -1;
            tickTimer = new Stopwatch();
            tickTimer.Start();
            JASPtr = sys;
            waveCache = new Dictionary<int, JAIDSPSampleBuffer>();
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

        public unsafe static byte[] PCM8216BYTE(byte[] adpdata)
        {

            byte[] retBuff = new byte[adpdata.Length * 2];

            fixed (byte* BUFF = retBuff)
            {
                for (int sam = 0; sam < adpdata.Length; sam++) 
                {
                    var ww = (short)(adpdata[sam] * (adpdata[sam] < 0 ? 256 : 258));
                    retBuff[sam*2] = (byte)(ww & 0xFF);
                    retBuff[sam*2+1] = (byte)(ww >> 8);
                }
            }
            return retBuff;
        }



        /* Ugh god, this turned out more awful than i wanted it to. Redo this in the future. This currently has massive perf implications */
        public static JAIDSPSampleBuffer loadSound(int wsys_id, int waveID, out JWave data)
        {
            var cacheIndex = (wsys_id << 16) | waveID;
            data = null;
            JAIDSPSampleBuffer ret;
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
            byte[] pcm;
            JAIDSPSampleBuffer sbuf;

            switch (waveData.format)
            {
                case 0:
                    pcm = JAIDSPADPCM4.ADPCMToPCM16(ou, (JAIDSPADPCM4.ADPCMFormat)waveData.format);
                    if (waveData.loop)
                        sbuf = JAIDSP.SetupSoundBuffer(pcm, 1, (int)waveData.sampleRate, 16, (int)Math.Floor((waveData.loop_start / 8f) * 16f), (int)Math.Floor((waveData.loop_end / 8f) * 16f));
                    else
                        sbuf = JAIDSP.SetupSoundBuffer(pcm, 1, (int)waveData.sampleRate, 16);
                    break;
                case 2:
                    pcm = PCM8216BYTE(ou);
                    if (waveData.loop)
                        sbuf = JAIDSP.SetupSoundBuffer(pcm, 1, (int)waveData.sampleRate, 16, waveData.loop_start*2, waveData.loop_end*2);
                    else
                        sbuf = JAIDSP.SetupSoundBuffer(pcm, 1, (int)waveData.sampleRate, 16);
                    break;
                default:
                    sbuf = null;
                    break;

            }




            if (!Directory.Exists("wavout"))
                Directory.CreateDirectory("wavout");
            

  

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
            RuntimeMS = ts;
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
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("==DESYNC==\nSequence engine failed to complete a tick, now desync'd\n==DESYNC==");
                 
                    Console.ForegroundColor = w;

                    Console.WriteLine(E.ToString());
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

            var muteIdx = JaiSeqXLJA.findDynamicStringArgument("-mute", "none").Split(',');
            for (int i = 0; i < muteIdx.Length; i++)
            {
      
                if (muteIdx[i] != null && muteIdx[i] == id.ToString())
                    trk.muted = true;
            }
        }

    }
}
