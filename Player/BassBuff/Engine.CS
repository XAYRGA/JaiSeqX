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
        public static void Init()
        {
            Un4seen.Bass.BassNet.Registration("dane@xayr.ga", "2X932312433");
            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
            BassFx.LoadMe();
            BASS_DEVICEINFO info = new BASS_DEVICEINFO();
            for (int n = 0; Bass.BASS_GetDeviceInfo(n, info); n++)
            {
                Console.WriteLine(info.ToString());
            }
        }

    }
}
