using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libJAudio;
using SharpDX.XAudio2;
using SharpDX.Multimedia;
using SharpDX.XAPO.Fx;
using SharpDX.XAPO;

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
        private float envValueLast = 32261; // In case we only have a release for this voice. 
        private float[] pitchMatrix = { 1f, 1f, 1f };
        private float[] gain0Matrix = { 1f, 1f, 1f };
        private SourceVoice internalVoice;
        private int ticks;
        private int oscTicks;
        private float oscValue = 1f;
        private EffectDescriptor[] effectChain =  new EffectDescriptor[2]; // max 16 effects per voice
        private bool doDestroy = false;

        public JAIDSPVoice(ref JAIDSPSoundBuffer buff)
        {
            rootBuffer = buff;  // save root buffer.
            //Console.WriteLine(rootBuffer.format.Channels);
            internalVoice = new SourceVoice(JAIDSP.Engine, rootBuffer.format, VoiceFlags.None, 1024f); // create voice for mixer
            //internalVoice.
            internalVoice.SubmitSourceBuffer(rootBuffer.buffer, null); // flush buffer into mixer 
            internalVoice.SetOutputVoices(JAIDSP.VoiceDescriptor); // prolly wrong
            /*
            effectChain[(int)VoiceEffect.REVERB] = new EffectDescriptor(new Reverb(JAIDSP.Engine),buff.format.Channels);
            effectChain[(int)VoiceEffect.ECHO] = new EffectDescriptor(new Echo(JAIDSP.Engine),buff.format.Channels);
            internalVoice.SetEffectChain(effectChain);
            internalVoice.DisableEffect((int)VoiceEffect.REVERB);
            internalVoice.DisableEffect((int)VoiceEffect.ECHO);       
            */
        }


        public void setPitch(float pitch)
        {
            pitchMatrix[0] = pitch;
            internalVoice.SetFrequencyRatio(pitch);
        }
        public void setVolume(float volume)
        {
            gain0Matrix[0] = volume;
        }


        public void play() {
            if (instOsc!=null && instOsc.envelopes[0]!=null)
            {
                swapEnvelope(instOsc.envelopes[0]);
            }
            internalVoice.Start();         
        }

        public void stop()
        {
            //Console.WriteLine("STOP");
           // internalVoice.Stop();
            
            if (instOsc==null)
            {
                doDestroy = true;
                internalVoice.Stop();
                return;
            }
            if (instOsc != null && instOsc.envelopes[1] == null)
            {
                doDestroy = true;
                internalVoice.Stop();
            }
            else
            {
                //Console.WriteLine("Set voice stop env!");
                swapEnvelope(instOsc.envelopes[1]);
            }
           // */
        }

        public void setOcillator(JOscillator osc)
        {
            instOsc = osc;
        }
        private void swapEnvelope(JEnvelope env)
        {
           
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
                    value = 0
                };
            }
            //*/
        }


        public byte updateVoice()
        {
            
            
            oscTicks++; // noooooooooooooooooooooooooooooooooooooooooooo
                        // please.

            if (doDestroy == true)
            {
                return 3;
            }

            float pv = 1f;
            for (int i = 0; i < pitchMatrix.Length;i++)
            {
                pv *= pitchMatrix[i];
            }
            internalVoice.SetFrequencyRatio(pv);
            float vv = 1f;
            for (int i = 0; i < gain0Matrix.Length; i++)
            {
                vv *= gain0Matrix[i];
            }
            internalVoice.SetVolume(vv);
       
            if (envCurrentVec==null)
            {
                return 2;
            }
            if (envCurrentVec.mode == JEnvelopeVectorMode.Stop)
            {
                internalVoice.Stop();
                doDestroy = true;
                return 3; // reeEEE EEE
            } else if (envCurrentVec.mode== JEnvelopeVectorMode.Hold) { // hold keeps the current value
                return 1;
            }
            if (envCurrentVec.next != null && envCurrentVec.next.time <= ticks)
            {
                envValueLast = envCurrentVec.value;
               // Console.WriteLine("SWAP ENV Last Value {3}\nNext Mode {0}\nCurrent Ticks {1}\nNext time {2}\nNext Value {4}", envCurrentVec.next.mode, ticks, envCurrentVec.next.time, envValueLast, envCurrentVec.next.value);
                envCurrentVec = envCurrentVec.next; // swap 
                return updateVoice();
            }
            //Console.WriteLine(envCurrentVec.value);
            var tickDist = envCurrentVec.next.time - envCurrentVec.time;

            var currentBaseTicks = envCurrentVec.time;
            //   Currently linear only implemented
            var mult = ((float)(ticks - currentBaseTicks) / (float)tickDist);
          
            envValue = (float)envValueLast + (float)(envCurrentVec.value - envValueLast) * mult;
            //Console.WriteLine(envValue);
            gain0Matrix[1] = envValue / 32758f;
            ticks++;
            return 1;
           // */
        }

        public void setEffectParams(VoiceEffect eff,params float[] parameters)
        {
            /*
            switch (eff)
            {
                case VoiceEffect.REVERB:
                {
                        internalVoice.EnableEffect((int)VoiceEffect.REVERB);
                        var w = internalVoice.GetEffectParameters<ReverbParameters>((int)VoiceEffect.REVERB);
                        w.RoomSize = parameters[0];
                        w.Diffusion = parameters[1];
                        internalVoice.SetEffectParameters<ReverbParameters>((int)VoiceEffect.REVERB, w);
                        break;
                }

                case VoiceEffect.ECHO:
                 {
                        internalVoice.EnableEffect((int)VoiceEffect.ECHO);
                        var w = internalVoice.GetEffectParameters<EchoParameters>((int)VoiceEffect.ECHO);
                        w.Delay = parameters[0];
                        w.Feedback = parameters[1];
                        w.WetDryMix = parameters[2];
                        internalVoice.SetEffectParameters<EchoParameters>((int)VoiceEffect.ECHO, w);
                        break;
                 }
            }
            */
        }


        /* There's a leak here. I have no clue what it is */
        public void Dispose() {

                internalVoice.Stop();
                internalVoice.DestroyVoice();
                internalVoice.Dispose();
        }
    }
}
