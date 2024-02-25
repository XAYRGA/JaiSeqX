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
using xayrga.JAIDSP;
using JaiSeqXLJA.Player;

namespace JaiSeqXLJA.DSP
{
    public enum VoiceEffect {
        REVERB = 0,
        ECHO = 1,
    }
    class JAIDSPVoice : IDisposable
    {
        public JAIDSPSampleBuffer rootBuffer;

        private JOscillator instOsc;
        private JEnvelopeVector envCurrentVec;

        public int fadeOutMS = 0;

        private float[] pitchMatrix = { 1f, 1f, 1f, 1f };
        private float[] gain0Matrix = { 1f, 1f, 1f, 1f };


        private int voiceHandle;
        private int syncHandle;


        private float ticks;
        public float tickAdvanceValue = 1;
        private int oscTicks;       
        private float oscValue = 32755f;
        public int addRelease = 0;
        public int stopFadeTime = 0;
        public int lastEnvValue;
        private short lastEnvValue2 = 0;
        private bool doDestroy = false;
        private bool crashed = false;
        private int toStop = 0;
        private bool tryStop = false;

        JAIDSPEnvelopeTemp currentEnv;

        public JAIDSPVoice(ref JAIDSPSampleBuffer buff)
        {
            rootBuffer = buff;  // save root buffer.

            voiceHandle = Bass.BASS_StreamCreateFile(buff.globalFileBuffer, 0, buff.fileBuffer.Length, BASSFlag.BASS_DEFAULT);

            if (buff.looped)
            {
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

        public void setPanning(float panning)
        {
            Bass.BASS_ChannelSetAttribute(voiceHandle, BASSAttribute.BASS_ATTRIB_PAN, panning);
        }

        public float getPitchMatrix(byte index)
        {
            return pitchMatrix[index];
        }
        public void setVolumeMatrix(byte index,float volume)
        {
            float envValue = 1f;
            if (currentEnv != null)
                envValue = currentEnv.fValue;          

            gain0Matrix[index] = volume;
            float vv = 1f;
            for (int i = 0; i < gain0Matrix.Length; i++)            
                vv *= gain0Matrix[i];
            
            Bass.BASS_ChannelSetAttribute(voiceHandle, BASSAttribute.BASS_ATTRIB_VOL,  vv *  envValue * envValue);
        }

        public void updateDSP()
        {

        }

        public void play(bool dbg = false) {
            if (instOsc!=null && instOsc.envelopes[0]!=null)
            {
                currentEnv = new JAIDSPEnvelopeTemp(instOsc.envelopes[0].vectorList, 0,dbg);
                //lastEnvValue2 = 32767;
            }
            Bass.BASS_ChannelPlay(voiceHandle,false);
        }

        public void forceStop()
        {
            destroy();
        }

        public void destroy()
        {
            Bass.BASS_ChannelStop(voiceHandle);
            Bass.BASS_StreamFree(voiceHandle);
            
            if (rootBuffer.looped)
            {
                Bass.BASS_ChannelRemoveSync(voiceHandle, syncHandle);
            }
    
        }
        public void stop()
        {

 
            if (instOsc != null && instOsc.envelopes != null && instOsc.envelopes.Length > 1 && instOsc.envelopes[1] != null)
            {
                if (instOsc.envelopes[1].vectorList[0] != null)
                {
                    if (currentEnv != null)
                        lastEnvValue2 = currentEnv.Value;

                    currentEnv = new JAIDSPEnvelopeTemp(instOsc.envelopes[1].vectorList, lastEnvValue2);
                }
                else
                {
                    var ww = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("JAIDSP");
                    Console.ForegroundColor = ww;
                    Console.WriteLine($"Instance {voiceHandle:X}  empty envelope vector");
                    FadeStop(50);
                }

            }
            else
            {
                var ww = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("JAIDSP");
                Console.ForegroundColor = ww;
                Console.WriteLine($": No envelope for voice {voiceHandle:X} FS(50)");
                FadeStop(50);
            }
     
        
            return;
        }

        public void stopImmediately()
        {
            Bass.BASS_ChannelRemoveSync(voiceHandle, syncHandle);
            Bass.BASS_StreamFree(voiceHandle);

        }
        public void setOcillator(JOscillator osc)
        {
            instOsc = osc;
        }
        private void swapEnvelope(JEnvelope env)
        {
  
            //*/
        }


        public byte updateVoice(double ms)
        {
       
            float envValue = 1;
            if ((currentEnv != null && currentEnv.update(JAISeqPlayer.timebaseValue * instOsc.Rate)) || doDestroy == true)
            {
                destroy();
                return 3;
            }
            else if (currentEnv != null)
            {
                lastEnvValue2 = currentEnv.Value;
                envValue = currentEnv.fValue * instOsc.Width + instOsc.Vertex;
            }
            
            float pv = 1f;
            for (int i = 0; i < pitchMatrix.Length;i++)
            {
                pv *= pitchMatrix[i];
            }
            Bass.BASS_ChannelSetAttribute(voiceHandle, BASSAttribute.BASS_ATTRIB_FREQ, rootBuffer.format.sampleRate * pv);
            float vv = 1f;
            for (int i = 0; i < gain0Matrix.Length; i++)
                vv *= gain0Matrix[i];

   
            Bass.BASS_ChannelSetAttribute(voiceHandle, BASSAttribute.BASS_ATTRIB_VOL,  vv * envValue * envValue);
            return 0;
        }

        public void setEffectParams(VoiceEffect eff,params float[] parameters)
        {

        }

        public void FadeStop(int miliseconds)
        {
            doDestroy = true;
            Bass.BASS_ChannelSlideAttribute(voiceHandle, BASSAttribute.BASS_ATTRIB_VOL, 0, miliseconds + addRelease);
            Bass.BASS_ChannelSetSync(voiceHandle, BASSSync.BASS_SYNC_SLIDE, 0, JAIDSP.globalFadeFreeProc, new IntPtr(0));

        }


        /* There's a leak here. I have no clue what it is */
        public void Dispose() {

           
        }
    }
}