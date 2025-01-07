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
using System.Diagnostics;

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

        public float fadeOutMS = 0;
        public float fadeOutMSInit = 0;

        private float[] pitchMatrix = { 1f, 1f, 1f, 1f };
        private float[] gain0Matrix = { 1f, 1f, 1f, 1f };
        private float[] panMatrix = { 64, 64, 64, 64 };

        private int voiceHandle;
        private int syncHandle;
        private int fxHandle;


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
        public bool debug = false;
        public int attack;
        public int release;
        public bool manual_attack_release = false;

        JAIDSPEnvelope currentEnv;

        
        public BASS_BFX_FREEVERB reverbSettings = new BASS_BFX_FREEVERB()
        {
            fDamp = JaiSeqXLJA.fDamp,
            fDryMix = JaiSeqXLJA.fDryMix,
            fRoomSize = JaiSeqXLJA.fRoomSize,
            //fWetMix = JaiSeqXLJA.fWetMix,
            fWidth = JaiSeqXLJA.fWidth,
        };

        public JAIDSPVoice(ref JAIDSPSampleBuffer buff)
        {
            rootBuffer = buff;  // save root buffer.


            
            voiceHandle = Bass.BASS_StreamCreateFile(buff.globalFileBuffer, 0, buff.fileBuffer.Length, BASSFlag.BASS_DEFAULT);
            fxHandle = Bass.BASS_ChannelSetFX(voiceHandle, BASSFXType.BASS_FX_BFX_FREEVERB, 1);
            Bass.BASS_FXSetParameters(fxHandle, reverbSettings); 
  

            if (buff.looped)
                syncHandle = Bass.BASS_ChannelSetSync(voiceHandle, BASSSync.BASS_SYNC_POS | BASSSync.BASS_SYNC_MIXTIME | BASSSync.BASS_SYNC_THREAD | BASSSync.BASS_SYNC_MIXTIME, buff.loopEnd, JAIDSP.globalLoopProc, new IntPtr(buff.loopStart));
        }

        public void setPitchMatrix(byte index,float pitch)
        {
            pitchMatrix[index] = pitch;
            float pv = 1f;
            for (int i = 0; i < pitchMatrix.Length; i++)
                pv *= pitchMatrix[i];

            Bass.BASS_ChannelSetAttribute(voiceHandle, BASSAttribute.BASS_ATTRIB_FREQ,rootBuffer.format.sampleRate * pv);
        }

        public void setPanMatrix(byte index, float pan)
        {
            panMatrix[index] = pan;
            var panValue = 1f;
            for (int i = 0; i < panMatrix.Length; i++)
                panValue *= ((panMatrix[i]) / 64f);

            panValue = panValue;
            Bass.BASS_ChannelSetAttribute(voiceHandle, BASSAttribute.BASS_ATTRIB_PAN,  1f - panValue  );
        }
        
        public void setReverb(float reverb)
        {
     
            reverbSettings.fWetMix = reverb * JaiSeqXLJA.fWetMix;
            Bass.BASS_FXSetParameters(fxHandle, reverbSettings);
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

            var finalVol = (vv) * envValue;
          
            
            Bass.BASS_ChannelSetAttribute(voiceHandle, BASSAttribute.BASS_ATTRIB_VOL, finalVol);
        }



        public void play(bool dbg = false) {
            if (instOsc!=null && instOsc.envelopes[0]!=null)
                currentEnv = new JAIDSPEnvelope(instOsc.envelopes[0].vectorList, 0,dbg);

            Bass.BASS_ChannelPlay(voiceHandle,false);
            debug = dbg;
        }

        public void forceStop()
        {
            destroy();
        }

        public void destroy()
        {
            Bass.BASS_ChannelRemoveFX(fxHandle, 0);
            Bass.BASS_ChannelStop(voiceHandle);
            Bass.BASS_StreamFree(voiceHandle);
    
            
            if (rootBuffer.looped)
                Bass.BASS_ChannelRemoveSync(voiceHandle, syncHandle);
        }
        static sbyte errCnt = 0;
        public void stop()
        {

            if (instOsc != null && instOsc.envelopes != null && instOsc.envelopes.Length > 1 && instOsc.envelopes[1] != null)
            {
                if (instOsc.envelopes[1].vectorList[0] != null)
                {
                    if (currentEnv != null)
                        lastEnvValue2 = currentEnv.Value;

                    currentEnv = new JAIDSPEnvelope(instOsc.envelopes[1].vectorList, lastEnvValue2);
            
                }
                else
                {
                    var ww = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("JAIDSP");
                    Console.ForegroundColor = ww;
                    Console.WriteLine($"Instance {voiceHandle:X}  empty envelope vector");
                    FadeStop(100);
                }
            } else if (manual_attack_release)
            {
                FadeStop(release);
            }
            else
            {
                FadeStop(100);
            }
     
        
            return;
        }

        public void stopImmediately()
        {
            Console.WriteLine("Stopping immediately!");
            FadeStop(100);

        }
        public void setOcillator(JOscillator osc)
        {
            instOsc = osc;
        }

        public void setAttack(int mils)
        {
            attack = mils;
            manual_attack_release = true;
        }

        public void setRelease(int mills)
        {
            release = mills;
            manual_attack_release = true;
        }

        public byte updateVoice(double ms)
        {

            float envValue = 1;
            fadeOutMS -= (float)ms;

            if (((currentEnv != null && currentEnv.update(ms * instOsc.Rate )) || doDestroy == true) && ! tryStop )
            {     
                destroy();
                return 3;
            }
            else if (currentEnv != null && !tryStop)
            {
                lastEnvValue2 = currentEnv.Value;
                envValue = (currentEnv.fValue * instOsc.Width) + instOsc.Vertex;
            } else if (tryStop)
            {
                envValue = fadeOutMS / fadeOutMSInit;
                if (fadeOutMS <= 0)
                {
                    destroy();
                    return 3;
                }
            }
      
            float pv = 1f;
            for (int i = 0; i < pitchMatrix.Length;i++)
                pv *= pitchMatrix[i];            
            Bass.BASS_ChannelSetAttribute(voiceHandle, BASSAttribute.BASS_ATTRIB_FREQ, rootBuffer.format.sampleRate * pv);

            float vv = 1f;
            for (int i = 0; i < gain0Matrix.Length; i++)
                vv *= gain0Matrix[i];   
            Bass.BASS_ChannelSetAttribute(voiceHandle, BASSAttribute.BASS_ATTRIB_VOL,  vv * envValue);

            var panValue = 1f;
            for (int i = 0; i < panMatrix.Length; i++)
                panValue *= ((panMatrix[i]) / 64f);
            panValue = (float)Math.Sqrt(panValue);

            Bass.BASS_ChannelSetAttribute(voiceHandle, BASSAttribute.BASS_ATTRIB_PAN,  panValue -1f );

            return 0;
        }

        public void FadeStop(int miliseconds)
        {
            fadeOutMS = miliseconds;
            fadeOutMSInit = miliseconds;
            tryStop = true;
        }


        /* There's a leak here. I have no clue what it is */
        public void Dispose() {

           
        }
    }
}