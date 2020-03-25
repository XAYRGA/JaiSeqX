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

        private static JAIDSPSoundBuffer[] waveCache;


        public static void startPlayback(string file, ref JASystem sys)
        {
            var contents = File.ReadAllBytes(file);
            tracks[0] = new JAISeqTrack(ref contents,0x0000000); // entry point.
            tickTimer = new Stopwatch();
            tickTimer.Start();
            recalculateTimebase();
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
                    Console.WriteLine("SEQUENCER MISSED TICK");
                    Console.WriteLine(E.ToString());
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
                }
            }
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
