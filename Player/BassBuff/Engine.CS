using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;

namespace JaiSeqX.Player.BassBuff
{
    public static class Engine
    {
        public static SYNCPROC globalLoopProc;
        public static SYNCPROC g_FadeFreeProc;

        public static void Init()
        {
            #region dumb obfuscation for email and registration key, just to prevent bots.
            byte obfu = 0xDA;
            byte[] eml = new byte[]
            {
                0xBE, 0xBB, 0xB4,0xBF,0x9A,0xA2,0xBB,0xA3,0xA8,0xF4,0xBD,0xBB,
            };

            byte[] rkey = new byte[]
            {
                0xE8,0x82,0xE3,0xE9,0xE8,0xE9,0xEB,0xE8,0xEE,0xE9,0xE9,
            };
            for (int i=0; i < eml.Length;i++)
            {
                eml[i] ^=(obfu);
            }
            for (int i = 0; i < rkey.Length; i++)
            {
                rkey[i] ^= (obfu);
            }
            #endregion

            Un4seen.Bass.BassNet.Registration(Encoding.ASCII.GetString(eml), Encoding.ASCII.GetString(rkey)); // Registration code, feel free to email me.  
            // Note that because of this, JaiSeqX cannot be used for commercial purposes.
            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero); // Initialize audio engine
            BassFx.LoadMe(); // Load the effects library

            globalLoopProc = new SYNCPROC(DoLoop); // Create our loop proc to bind audio objects to, global and static so it doesn't get collected.
            g_FadeFreeProc = new SYNCPROC(FadeCollect);


            BASS_DEVICEINFO info = new BASS_DEVICEINFO(); // Print device info. 
            for (int n = 0; Bass.BASS_GetDeviceInfo(n, info); n++)
            {
                Console.WriteLine(info.ToString());
            }
        }

 
        private static void DoLoop(int syncHandle, int channel, int data, IntPtr user)
        {        
                Bass.BASS_ChannelSetPosition(channel, user.ToInt64());
        }

        private static void FadeCollect(int syncHandle, int channel, int data, IntPtr user)
        {
            Bass.BASS_ChannelRemoveSync(channel, syncHandle);
            Bass.BASS_StreamFree(channel);
            //Console.Write("Dealloc {0}", channel);
           
        }

    }
}
