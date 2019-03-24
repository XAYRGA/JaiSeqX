using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.XAudio2;
using SharpDX.Multimedia;
using SharpDX.XAPO.Fx;
using System.IO;

namespace XAYRGA.SharpSL
{
    public class SoundEffectInstance : IDisposable
    {

        private AudioBuffer sBuffer;


        private float iPitch = 0;
        private float iVolume = 1;

        private WaveFormat fmt;
        private SourceVoice svoice;
        private SubmixVoice myMixer;

        bool playing = false;

 

        public SoundEffectInstance(AudioBuffer bdata, WaveFormat wft, SubmixVoice mix)
        {

            fmt = wft;
            sBuffer = bdata;
            svoice = new SourceVoice(SharpSLEngine.GetEngineBase(), fmt, VoiceFlags.None, 1024f);
            svoice.SubmitSourceBuffer(sBuffer, null);

            myMixer = mix;
            svoice.SetOutputVoices(new VoiceSendDescriptor(myMixer));
           
        }

        public void Play()
        {
            playing = true;

            lock (myMixer)
            {
                lock (svoice)
                {
                    svoice.Start();
                }
            }

        }

        public void Stop()
        {
            playing = false;

            lock (myMixer)
            {
                lock (svoice)
                {
                    svoice.Stop();
                }
            }
        }

  

        public float Pitch
        {
            get
            {
                return iPitch;
            }
            set
            {
                svoice.SetFrequencyRatio(value);
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
                svoice.SetVolume(value);
                iVolume = value;


            }
        }

        public void Dispose()
        {
            svoice.Dispose();

        }

        ~SoundEffectInstance()
        {
            Dispose();
        }
    }
}
