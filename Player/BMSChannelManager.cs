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

        public bool addVoice(int index,SoundEffectInstance voice) // to add a voice to a channel
        {
            if (index < voices.Length) // Make sure the index isn't stupid.
            {
                if (voices[index]!=null) // Check if we already have a sound playing there.
                {
                    stopVoice(index); // Stop it if we do. 
                }
                voices[index] = voice; // Throw the voice into its index
                return true; // success
            }
            return false; 
        }

        public bool stopVoice(int index)
        {
            if (index < voices.Length) { // Make sure the index isnt stupid
                if (voices[index] != null) // dont do any work if we dont have anything to do
                {
                    var voi = voices[index]; // grab the voice
                    voi.Stop(); // Stop the voice
                    voi.Dispose(); // feed it to GC
                    voices[index] = null; // clear its index in the voice table
                    
                    return true; // good
                }
            }
            return false; // we didnt do anything
        }

    }

    public class BMSChannelManager
    {
        SoundEffect[] Cache; // Sound cache
        string[] CacheStrings; // Maps the sound path to the cache index 
        BMSChannel[] channels; // current channels
        int cacheHigh; // Highest number in the cache we have

        public BMSChannelManager()
        {
            SharpSLEngine.Init();
            Cache = new SoundEffect[1024]; // I HOPE that the engine doesn't need more than 1024 sounds at once.
            CacheStrings = new string[1024]; // ^
            channels = new BMSChannel[32];  // Usually no more than 16 channels.  Again, just to be safe

            for (int i=0; i < channels.Length;i++)
            {
                channels[i] = new BMSChannel();  // Preallocating the channels
            }

        }

        public SoundEffect loadSound(string file, bool lo, int ls, int le)
        {
            for (int i=0; i < CacheStrings.Length;i++)
            {
                if (CacheStrings[i]==null || i > cacheHigh) // if we've hit null, we've hit the end of our array. 
                {
                    break;  // So just stop the loop
                } else
                {
                    if (CacheStrings[i]==file) // If we find it.
                    {
                        return Cache[i];  //  Return the same index of the cache (will be our file)
                    }
                } 
            }
#if DEBUG
            Console.WriteLine("loadSound {0}",file);

#endif
            CacheStrings[cacheHigh] = file; // otherwise, it's not loaded. so we need to store it in our cache
            Cache[cacheHigh] = new SoundEffect(file,lo,ls,le); // Load the WAV for it.
            var ret = Cache[cacheHigh];  // Then set our return value (we store it, because we increment cacheHigh below)
            cacheHigh++; // Increment our next cache index.

            return ret; // Return our object.
        }

        public void startVoice(SoundEffectInstance snd,byte channel,byte voice)
        {
            if (channels[channel]!=null) // check if the channel exists
            {
                var chn = channels[channel]; // if it does, reference it
                chn.addVoice(voice, snd); // store the voice
            }
        }

        public void stopVoice( byte channel, byte voice)
        {
            // see above, inverse.
            if (channels[channel] != null)
            {
                var chn = channels[channel];
                chn.stopVoice(voice);
            }
        }
    }
}
