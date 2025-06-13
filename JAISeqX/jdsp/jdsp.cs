using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Un4seen.Bass;
using Un4seen.Bass.Misc;
using Un4seen.Bass.AddOn.Fx;


namespace JAISeqX.jdsp
{
    
    static class jdsp
    {
        public static SYNCPROC loopProc;

        public static void init()
        {
            byte obfu = 0xDA;
            byte[] eml = new byte[] { 0xBE, 0xBB, 0xB4, 0xBF, 0x9A, 0xA2, 0xBB, 0xA3, 0xA8, 0xF4, 0xBD, 0xBB };
            byte[] rkey = new byte[] { 0xE8, 0x82, 0xE3, 0xE9, 0xE8, 0xE9, 0xEB, 0xE8, 0xEE, 0xE9, 0xE9 };
            for (int i = 0; i < eml.Length; i++)
                eml[i] ^= (obfu);

            for (int i = 0; i < rkey.Length; i++)
                rkey[i] ^= (obfu);

            BassNet.Registration(Encoding.ASCII.GetString(eml), Encoding.ASCII.GetString(rkey));
            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero); // Initialize audio engine
            loopProc = new SYNCPROC(DoLoop);
        }

        public static string[] GetDeviceList()
        {
            BASS_DEVICEINFO info = new BASS_DEVICEINFO(); // Print device info. 
            int n = 0;
            for (n = 0; Bass.BASS_GetDeviceInfo(n, info); n++) ;
            string[] devices = new string[n];
            for (int i = 0; Bass.BASS_GetDeviceInfo(i, info); i++)
                devices[i] = info.name;
            return devices;
        }

        private static void DoLoop(int syncHandle, int channel, int data, IntPtr user)
        {
            //Bass.BASS_ChannelSetPosition(channel, user.ToInt64(), BASSMode.BASS_POS;
        }

        public static bool Deinit() // free's engine thread
        {
            Bass.BASS_Free();
            return true;
        }

    }
}
