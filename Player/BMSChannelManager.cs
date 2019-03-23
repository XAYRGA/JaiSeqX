using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XAYRGA.SharpSL; 

namespace JaiSeqX.Player
{
    public class BMSChannel
    {
        SoundEffectInstance[] voices;
       
        public BMSChannel()
        {
            voices = new SoundEffectInstance[16]; // Should only ever have 8 voices, but still. 
        }

        public bool addVoice(int index,SoundEffectInstance voice)
        {

            if (index < voices.Length)
            {
                if (voices[index]!=null)
                {
                    stopVoice(index);

                    voices[index] = voice;
                }
                return true;
            }

            return false; 
        }

        public bool stopVoice(int index)
        {
            if (index < voices.Length) {
                if (voices[index] != null)
                {
                    var voi = voices[index];
                    voi.Stop();
                    voices[index] = null;
                    voi.Dispose();
                    return true;
                }
            }
            return false;
        }

    }

    public class BMSChannelManager
    {
        SoundEffect[] Cache;
        string[] CacheStrings;
        BMSChannel[] channels;
        int cacheHigh;

        public BMSChannelManager()
        {
            SharpSLEngine.Init(); // Start sound engine. 

            Cache = new SoundEffect[1024];
            CacheStrings = new string[1024];
            channels = new BMSChannel[32]; 

            for (int i=0; i < channels.Length;i++)
            {
                channels[i] = new BMSChannel(); 
            }

        }

        public SoundEffect loadSound(string file)
        {
            for (int i=0; i < CacheStrings.Length;i++)
            {
                if (CacheStrings[i]==null || i > cacheHigh)
                {
                    break; 
                } else
                {
                    if (CacheStrings[i]==file)
                    {
                        return Cache[i];
                    }
                } 
            }
            CacheStrings[cacheHigh] = file;
            Cache[cacheHigh] = new SoundEffect(file);
            var ret = Cache[cacheHigh]; 
            cacheHigh++;

            return ret;
        }

        public void startVoice(SoundEffectInstance snd,byte channel,byte voice)
        {
            if (channels[channel]!=null)
            {
                var chn = channels[channel];
                chn.addVoice(voice, snd);
            }
        }

        public void stopVoice( byte channel, byte voice)
        {
            if (channels[channel] != null)
            {
                var chn = channels[channel];
                chn.stopVoice(voice);
            }
        }
    }
}
