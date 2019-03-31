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
        int handle = 0;
      
        private float iPitch = 1;
        private float iVolume = 1;
        private SYNCPROC Proc;
        private int syncHandle;
        private float baseSRate = 0;
        private bool looping;

        public SoundEffectInstance(int bassHandle, bool loop, int loopstart, int loopend)
        {
            handle = bassHandle;

            Bass.BASS_ChannelGetAttribute(handle, BASSAttribute.BASS_ATTRIB_FREQ, ref baseSRate);
            
            
            if (loop)
            {
                Proc = new SYNCPROC(DoLoop);
                syncHandle = Bass.BASS_ChannelSetSync(handle, BASSSync.BASS_SYNC_POS | BASSSync.BASS_SYNC_MIXTIME, loopend, Proc, new IntPtr(loopstart));
            }
            looping = loop;
        }

        public float Pitch
        {
            get
            {
                return iPitch;
            }
            set
            {//
                Bass.BASS_ChannelSetAttribute(handle, BASSAttribute.BASS_ATTRIB_FREQ, baseSRate * value);
                iPitch = value;

            }
        }

        private void DoLoop(int syncHandle, int channel, int data, IntPtr user)
        {
            Bass.BASS_ChannelSetPosition(channel, user.ToInt64());
        }


        public float Volume
        {
            get
            {
                return iVolume;
            }
            set
            {
                Bass.BASS_ChannelSetAttribute(handle, BASSAttribute.BASS_ATTRIB_VOL, value);
                iVolume = value;
            }
        }
        public void Play()
        {
            Bass.BASS_ChannelSetAttribute(handle, BASSAttribute.BASS_ATTRIB_FREQ, baseSRate * iPitch);
           // Console.WriteLine(Bass.BASS_ErrorGetCode());
            Bass.BASS_ChannelPlay(handle, true);
            //Console.WriteLine(Bass.BASS_ErrorGetCode());


            // Bass.BASS_ChannelSetFX(tempoHandle, BASSFXType.BASS_FX_BFX_PITCHSHIFT, 1);



        }
        public void Stop()
        {
            Bass.BASS_ChannelStop(handle);
        }
        public void Dispose()
        {
            Stop();
            if (looping)
            {
                Bass.BASS_ChannelRemoveSync(handle, syncHandle);
            }
            Bass.BASS_StreamFree(handle);
           // Bass.BASS_StreamFree(tempoHandle);

        }
    }
}
