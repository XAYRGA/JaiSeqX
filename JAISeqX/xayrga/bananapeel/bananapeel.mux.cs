using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xayrga.bananapeel
{
    public static class mux
    {
        public static short[] PCM8216(byte[] adpdata)
        {
            short[] smplBuff = new short[adpdata.Length]; 
            for (int sam = 0; sam < adpdata.Length; sam++) 
                smplBuff[sam] = (short)(adpdata[sam] * (adpdata[sam] < 0 ? 256 : 258));
            
            return smplBuff;
        }

        public static byte[] PCM1628(short[] pcm16)
        { 
            byte[] pcm8Data = new byte[pcm16.Length];
            for (int ix = 0; ix < pcm8Data.Length; ix++)            
                pcm8Data[ix] = (byte)((sbyte)(pcm16[ix] >> 8));
            
            return pcm8Data;
        }

        public static short[] PCM16BYTESWAP(short[] pcm)
        {
            var PCM16 = new short[pcm.Length];
                for (int i = 0; i < PCM16.Length; i++)
                {
                    var oPCM = (ushort)pcm[i];
                    PCM16[i] = (short)((oPCM & 0xFF << 8) | (oPCM >> 8));    
                }
            return PCM16;
        }

        public unsafe static short[] PCM16ByteToShort(byte[] pcm)
        {
            var pcmS = new short[(pcm.Length + 2 - 1) / 2];
            fixed (byte* pcmD = pcm)
            {
                var pcmBy = (short*)pcmD;
                for (int i = 0; i < pcmS.Length; i++)
                    pcmS[i] = pcmBy[i];                
            }
            return pcmS;
        }

        public static byte[] PCM16ShortToByte(short[] pcm)
        {
            byte[] outbytes = new byte[pcm.Length * 2];
            for (int i=0; i < pcm.Length; i++)
            {
                var sample = pcm[i];
                outbytes[i * 2] =  (byte)(sample >> 8);
                outbytes[i * 2 + 1] = (byte)(sample & 0xFF);
            }
            return outbytes;
        }


        public static byte[] PCM16TOADPCM4(short[] pcm)
        {
            var totalFrames = (pcm.Length + 16 - 1) / 16; // upwards frame rounding. 
            var adpcmData = new byte[totalFrames * 9]; // 9 bytes per ADPCM frame
            var frame = 0;
            var last = 0;
            var penult = 0;

            for (int sample = 0; sample < pcm.Length; sample+=16)
            {
                var frameSamples = new short[16];
                var adpcm = new byte[9];
                Array.Copy(pcm, sample, frameSamples, 0, 16);
                bananapeel.ADPCMFAST.PCM16TOADPCM4(frameSamples, adpcm, ref last, ref penult);
                Array.Copy(adpcm, 0, adpcmData, frame * 9, 9);
                frame++;
            }
            return adpcmData;
        }


        public static byte[] PCM16TOADPCM4(short[] pcm, int loopSmpl, out short loopLast, out short loopPenult)
        {
            var totalFrames = (pcm.Length + 16 - 1) / 16; // upwards frame rounding. 
            var adpcmData = new byte[totalFrames * 9]; // 9 bytes per ADPCM frame
            var frame = 0;
            var last = 0;
            var penult = 0;
            loopLast = 0;
            loopPenult = 0;

            for (int sample = 0; sample < pcm.Length; sample += 16)
            {
                var frameSamples = new short[16];
                var adpcm = new byte[9];
                if (sample == loopSmpl || sample + 16 < loopSmpl)
                {
                    loopLast = (short)last;
                    loopPenult = (short)penult;
                }
                Array.Copy(pcm, sample, frameSamples, 0, 16);
                bananapeel.ADPCMFAST.PCM16TOADPCM4(frameSamples, adpcm, ref last, ref penult);
                Array.Copy(adpcm, 0, adpcmData, frame * 9, 9);
                frame++;
            }
            return adpcmData;
        }

        public static short[] ADPCM4TOPCM16(byte[] adpdata)
        {
            var totalSamples = ((adpdata.Length / 9) * 16);
            var frameOffset = 0; 
            short[] smplBuff = new short[totalSamples];
            int pen = 0; 
            int last = 0; 
            for (int sam = 0; sam < totalSamples; sam += 16) 
            {
                byte[] adpcm4 = new byte[9]; 
                short[] sample = new short[16]; 
                for (int i = 0; i < 9; i++) 
                    adpcm4[i] = adpdata[frameOffset + i];
                frameOffset += 9; 
                bananapeel.ADPCMFAST.ADPCM4TOPCM16(adpcm4, sample, ref last, ref pen); 
                for (int i = 0; i < 16; i++)  
                    smplBuff[sam + i] = sample[i]; 
            }
            return smplBuff; // return
        }


        public static short[] ADPCM2TOPCM16(byte[] adpdata)
        {
            var totalSamples = ((adpdata.Length / 5) * 16);
            var frameOffset = 0;
            short[] smplBuff = new short[totalSamples];
            int pen = 0;
            int last = 0;
            for (int sam = 0; sam < totalSamples; sam += 16)
            {
                byte[] adpcm4 = new byte[5];
                short[] sample = new short[16];
                for (int i = 0; i < 5; i++)
                    adpcm4[i] = adpdata[frameOffset + i];
                frameOffset += 5;
                bananapeel.ADPCMFAST.ADPCM4TOPCM16(adpcm4, sample, ref last, ref pen);
                for (int i = 0; i < 16; i++)
                    smplBuff[sam + i] = sample[i];
            }
            return smplBuff; // return
        }

    }
}
