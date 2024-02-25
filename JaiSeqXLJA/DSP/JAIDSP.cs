﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Un4seen.Bass;
using Un4seen.Bass.Misc;
using Un4seen.Bass.AddOn.Fx;

namespace JaiSeqXLJA.DSP
{
    public static class JAIDSP
    {

        public static SYNCPROC globalLoopProc;
        public static SYNCPROC globalFadeFreeProc;
        public static bool Init()
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
            for (int i = 0; i < eml.Length; i++)
            {
                eml[i] ^= (obfu);
            }
            for (int i = 0; i < rkey.Length; i++)
            {
                rkey[i] ^= (obfu);
            }
            #endregion
            Un4seen.Bass.BassNet.Registration(Encoding.ASCII.GetString(eml), Encoding.ASCII.GetString(rkey));
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_SRC, 6); //  SINC POLYPOINT 128
            Bass.BASS_Init(JaiSeqXLJA.findDynamicNumberArgument("-jdsp.device", -1), 48000, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero); // Initialize audio engine

    

            BassFx.LoadMe();
            BASS_DEVICEINFO info = new BASS_DEVICEINFO(); // Print device info. 
            for (int n = 0; Bass.BASS_GetDeviceInfo(n, info); n++)
            {
                Console.WriteLine($"{n} = {info.ToString()} ");
            }
            
            


            globalLoopProc = new SYNCPROC(DoLoop);
            globalFadeFreeProc = new SYNCPROC(FadeCollect);
            return true;
        }
        public static bool Deinit() // free's engine thread
        {
            Bass.BASS_Free();
            BassFx.FreeMe();
            return true;
        }

        private static void DoLoop(int syncHandle, int channel, int data, IntPtr user)
        {
            Bass.BASS_ChannelSetPosition(channel, user.ToInt64(),BASSMode.BASS_POS_BYTE);
        }
        private static int v1 = 0;
        public static JAIDSPSampleBuffer SetupSoundBuffer(byte[] pcm, int cn, int sr, int bs, int ls, int le)
        {
            v1++;
            var rt = new JAIDSPSampleBuffer()
            {
                format = new JAIDSPFormat()
                {
                    channels = cn,
                    sampleRate = sr,
                },
                buffer = pcm,
                loopStart = ls,
                loopEnd = le,
                looped = true,
            };
            rt.generateFileBuffer();
            //Console.WriteLine($"Dumping sample {v1}");
            //File.WriteAllBytes("test/" + v1.ToString() + ".wav", rt.fileBuffer) ;
            return rt;
        }



        private static void FadeCollect(int syncHandle, int channel, int data, IntPtr user)
        {
            Bass.BASS_ChannelRemoveSync(channel, syncHandle);
            Bass.BASS_StreamFree(channel);


        }

        public static JAIDSPSampleBuffer SetupSoundBuffer(byte[] pcm, int cn, int sr, int bs)
        {
            v1++;
            
            var rt = new JAIDSPSampleBuffer()
            {
                format = new JAIDSPFormat()
                {
                    channels = cn,
                    sampleRate = sr,
                },
                buffer = pcm,
                looped = false,
            };
            rt.generateFileBuffer();
            //Console.WriteLine($"Dumping sample {v1}");
            //File.WriteAllBytes("test/" + v1.ToString() + ".wav", rt.fileBuffer);
            return rt;
        }
    }
}
