using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libJAudio;
using Un4seen.Bass;
using Un4seen.Bass.Misc;
using Un4seen.Bass.AddOn.Fx;
using System.IO;
using System.Runtime.InteropServices;

namespace JaiSeqXLJA.DSP
{
    public enum VoiceEffect {
        REVERB = 0,
        ECHO = 1,
    }
    class JAIDSPVoice : IDisposable
    {
        public JAIDSPSoundBuffer rootBuffer;

        private JOscillator instOsc;
        private JEnvelopeVector envCurrentVec;
        private float envValue = 1;
        private float envValueLast = 1; // In case we only have a release for this voice. 

        private float[] pitchMatrix = { 1f, 1f, 1f };
        private float[] gain0Matrix = { 1f, 1f, 1f };

        private int voiceHandle;
        private int syncHandle;


        private float ticks;
        public float tickAdvanceValue = 1;
        private int oscTicks;       
        private float oscValue = 32755f;

        private bool doDestroy = false;
        private bool crashed = false;

        public JAIDSPVoice(ref JAIDSPSoundBuffer buff)
        {
            rootBuffer = buff;  // save root buffer.


            voiceHandle = Bass.BASS_StreamCreateFile(buff.globalFileBuffer, 0, buff.fileBuffer.Length, BASSFlag.BASS_DEFAULT);

            if (buff.looped)
            {
                //Console.WriteLine("Force loop!");
                syncHandle = Bass.BASS_ChannelSetSync(voiceHandle, BASSSync.BASS_SYNC_POS | BASSSync.BASS_SYNC_MIXTIME , buff.loopEnd, JAIDSP.globalLoopProc, new IntPtr(buff.loopStart));
            }
        }

        public void setPitchMatrix(byte index,float pitch)
        {
            pitchMatrix[index] = pitch;
            //internalVoice.SetFrequencyRatio(pitch);
            float pv = 1f;
            for (int i = 0; i < pitchMatrix.Length; i++)
            {
                pv *= pitchMatrix[i];
            }
            Bass.BASS_ChannelSetAttribute(voiceHandle, BASSAttribute.BASS_ATTRIB_FREQ,rootBuffer.format.sampleRate * pv);
        }

        public float getPitchMatrix(byte index)
        {
            return pitchMatrix[index];
        }
        public void setVolumeMatrix(byte index,float volume)
        {
            gain0Matrix[index] = volume;
            float vv = 1f;
            for (int i = 0; i < gain0Matrix.Length; i++)
            {
                vv *= gain0Matrix[i];
            }
            //Console.WriteLine("Final Gain {0}", vv);
            Bass.BASS_ChannelSetAttribute(voiceHandle, BASSAttribute.BASS_ATTRIB_VOL,  vv);
        }


        public void play() {
            if (instOsc!=null && instOsc.envelopes[0]!=null)
            {
                swapEnvelope(instOsc.envelopes[0]);
            }
            Bass.BASS_ChannelPlay(voiceHandle,false);
        }

        public void forceStop()
        {
            destroy();
        }

        public void destroy()
        {
            //Bass.BASS_ChannelStop(voiceHandle);
            //Bass.BASS_StreamFree(voiceHandle);
            
            if (rootBuffer.looped)
            {
                Bass.BASS_ChannelRemoveSync(voiceHandle, syncHandle);
            }
    
        }
        public void stop()
        {
            doDestroy = true;
            FadeStop(130);
            
               destroy();
                return;
        }

        public void setOcillator(JOscillator osc)
        {
            instOsc = osc;
        }
        private void swapEnvelope(JEnvelope env)
        {
            ticks = 0;
            var rqVec = env.vectorList[0];
            if (rqVec.time == 0)
            {
                envCurrentVec = rqVec;  // This could be wrong, some envelopes might not have an initializer value.
                envValue = envCurrentVec.value; // i hope
                envValueLast = envCurrentVec.value;

            } else { // the latter case, here, should never happen. But in case it does, here's a failsafe. 
                envCurrentVec = new JEnvelopeVector()
                {
                    mode = rqVec.mode,
                    next = rqVec,
                    time = 0,
                    value = 32700
                };
            }
            //*/
        }


        public byte updateVoice()
        {
            oscTicks++; 

            if (doDestroy == true)
            {
                return 3;
            }

            float pv = 1f;
            for (int i = 0; i < pitchMatrix.Length;i++)
            {
                pv *= pitchMatrix[i];
            }
            Bass.BASS_ChannelSetAttribute(voiceHandle, BASSAttribute.BASS_ATTRIB_FREQ, rootBuffer.format.sampleRate * pv);
            float vv = 1f;
            for (int i = 0; i < gain0Matrix.Length; i++)
            {
                vv *= gain0Matrix[i];
            }
   
            Bass.BASS_ChannelSetAttribute(voiceHandle, BASSAttribute.BASS_ATTRIB_VOL,  vv);
            return 0;
        }

        public void setEffectParams(VoiceEffect eff,params float[] parameters)
        {

        }

        public void FadeStop(int miliseconds)
        {
            Bass.BASS_ChannelSlideAttribute(voiceHandle, BASSAttribute.BASS_ATTRIB_VOL, 0, miliseconds);
            Bass.BASS_ChannelSetSync(voiceHandle, BASSSync.BASS_SYNC_SLIDE, 0, JAIDSP.globalFadeFreeProc, new IntPtr(0));

        }


        /* There's a leak here. I have no clue what it is */
        public void Dispose() {

           
        }
    }
}