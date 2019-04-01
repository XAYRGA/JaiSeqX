using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;

namespace JaiSeqX.Player.BassBuff
{
    public class SoundEffectInstance : IDisposable
    {
        int handle = 0; // BASS Handle
      
        private float iPitch = 1;
        private float iVolume = 1;

        private int syncHandle;
        private float baseSRate = 0;
        private bool looping;

        private bool fading;

        public bool ShouldFade = false;
        public int FadeOutMS = 0;

        public SoundEffectInstance(int bassHandle, bool loop, int loopstart, int loopend)
        {
            handle = bassHandle; // Store the handle
            Bass.BASS_ChannelGetAttribute(handle, BASSAttribute.BASS_ATTRIB_FREQ, ref baseSRate); // Store the original sample rate (for pitch bending)
            var sz = Bass.BASS_ChannelGetLength(handle);
            
            if (loopend > sz | (loopend - loopstart) < 3000) // Hax until i figure out wtf is going on with looping. 
            {
               loopend = (int)sz;
             
            }
            //Console.WriteLine("{0} {1}", loopstart, loopend);
            if (loop) // If we loop
            {
                syncHandle = Bass.BASS_ChannelSetSync(handle, BASSSync.BASS_SYNC_POS | BASSSync.BASS_SYNC_MIXTIME, loopend, Engine.globalLoopProc, new IntPtr(loopstart));// Set the global loop proc to take place at the loop end position, then return to the start.
            }
            looping = loop; // Loopyes
        }

        public float Pitch
        {
            get
            {
                return iPitch;
            }
            set
            {//
                Bass.BASS_ChannelSetAttribute(handle, BASSAttribute.BASS_ATTRIB_FREQ, baseSRate * value); // Change the frequency of the sound
                iPitch = value;

            }
        }
   


        public float Volume
        {
            get
            {
                return iVolume;
            }
            set
            {
                Bass.BASS_ChannelSetAttribute(handle, BASSAttribute.BASS_ATTRIB_VOL, value); // Change the volume of the sound
                iVolume = value;
            }
        }
        public void Play()
        {
            Bass.BASS_ChannelSetAttribute(handle, BASSAttribute.BASS_ATTRIB_FREQ, baseSRate * iPitch); // For good measure, unsure if needed.
            Bass.BASS_ChannelPlay(handle, true); // Tell it to play


        }

        public void FadeStop(int miliseconds)
        {
            fading = true;
            Bass.BASS_ChannelSlideAttribute(handle, BASSAttribute.BASS_ATTRIB_VOL, 0, miliseconds);
            Bass.BASS_ChannelSetSync(handle, BASSSync.BASS_SYNC_SLIDE, 0, Engine.g_FadeFreeProc, new IntPtr(0));
             
        }

        public void Stop()
        {
            //Bass.BASS_ChannelSlideAttribute(handle, BASSAttribute.BASS_ATTRIB_VOL, 0F, 1000);
            if (ShouldFade)
            {
                FadeStop(FadeOutMS);
                return;
            }
            Bass.BASS_ChannelStop(handle); // Tell it to stop
        }
        public void Dispose() // Let the object dispose. 
        {
            if (!fading)
            {
                Stop(); // If it's being collected, stop it first.
              
                Bass.BASS_StreamFree(handle); // Then finally, we can free the stream, as the sound is no longer used in any way. 
            }
            // Let the GC do its thing, i guess. 
            if (looping) // If it loops
            {
                // We need to deallocate the sync proc
                Bass.BASS_ChannelRemoveSync(handle, syncHandle);
            }

        }

    }
}
 