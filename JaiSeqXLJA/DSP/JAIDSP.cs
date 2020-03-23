using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.XAPO.Fx;
using SharpDX.Multimedia; 
using SharpDX.XAudio2;
using System.IO;
//** Formerly SharpSL 


namespace JaiSeqXLJA.DSP
{
    public static class JAIDSP
    {
        private static XAudio2 RootContext;
        public static XAudio2 Engine { get { return RootContext; }  private set { }  }
        public static MasteringVoice FOutput; 

        public static bool Init()
        {
            RootContext = new XAudio2();
            FOutput = new MasteringVoice(RootContext);
            return true;
        }

        public static SubmixVoice Mixer
        {
            get
            {
                var Mixer = new SubmixVoice(RootContext);
                return Mixer;
            }
            private set { }
        }

        public static VoiceSendDescriptor VoiceDescriptor
        {
            get
            {
                var Mixer = new VoiceSendDescriptor(JAIDSP.Mixer);
                return Mixer;
            }
            private set { }
        }

        public static XAudio2 GetEngineBase()
        {
            return RootContext;
        }


        public static JAIDSPSoundBuffer SetupSoundBuffer(byte[] pcm, int cn, int sr, int bs, int ls, int le)
        {
            var abuff = new AudioBuffer();
            abuff.AudioBytes = pcm.Length;
            abuff.Stream = new SharpDX.DataStream(pcm.Length, true, true);
            abuff.Stream.Write(pcm, 0, pcm.Length);
            abuff.PlayBegin = 0;
            abuff.PlayLength = 0;
            abuff.LoopCount = 255;
            abuff.LoopBegin = ls;
            abuff.LoopLength = le - ls;
            var rt = new JAIDSPSoundBuffer()
            {
                format = new WaveFormat(sr,cn),
                buffer = abuff,
            };
            return rt;
        }

        public static JAIDSPSoundBuffer SetupSoundBuffer(byte[] pcm, int cn, int sr, int bs)
        {
            var abuff = new AudioBuffer();
            abuff.AudioBytes = pcm.Length;
            abuff.Stream = new SharpDX.DataStream(pcm.Length, true, true);
            abuff.Stream.Write(pcm, 0, pcm.Length);
            abuff.PlayBegin = 0;
            abuff.PlayLength = 0;
            var rt = new JAIDSPSoundBuffer()
            {
                format = new WaveFormat(sr, cn),
                buffer = abuff,
            };
            return rt;
        }
    }
}
