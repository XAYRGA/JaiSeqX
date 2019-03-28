using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.XAudio2;
using SharpDX.Multimedia;
using SharpDX.XAPO.Fx;
using System.IO;
// Looping Hack: 
/*
 * 
 * int repeats = 2;
AL10.alSourcei(sourceID, AL10.AL_LOOPING, AL10.AL_FALSE);
for(int i = 0; i < repeats; i++)
    AL10.alSourceQueueBuffers(sourceID, bufferID);
AL10.alSourcePlay(sourceID);

Maybe I can use this to hack a sort of loop, just queue 64 buffers of the loop buffer perhaps?  it would emulate looping, OpenAL wont know any better. 

    Being that jaudio sounds usually go to the end, i could probably just sammich the buffer onto the end. 
 * 
 */
 
namespace XAYRGA.SharpSL
{
    public class SoundEffect : IDisposable
    {


        private byte[] bufferData;
        private AudioBuffer sBuffer; 

        private bool isLooped;

        private int LoopStart;
        private int LoopEnd; 


        private float iPitch = 0;
        private float iVolume = 1;

        private WaveFormat fmt;
        private SourceVoice svoice;

        private SubmixVoice myMixer; 

        bool playing = false;

        public static SoundEffect[] AllSounds = new SoundEffect[8192];
       

        public SoundEffect(string file) 
        {
            int channels, bits_per_sample, sample_rate;
            bufferData = SharpSLEngine.LoadWavFromFile(file, out channels, out bits_per_sample, out sample_rate);

            fmt = new WaveFormat(sample_rate, bits_per_sample, channels);
            sBuffer = new AudioBuffer();
            sBuffer.AudioBytes = bufferData.Length;
            sBuffer.Stream = new SharpDX.DataStream(bufferData.Length, true, true);
            sBuffer.Stream.Write(bufferData, 0, bufferData.Length);
            sBuffer.PlayBegin = 0;
            sBuffer.PlayLength = 0;
            sBuffer.LoopCount = (isLooped ? 255:0);
            sBuffer.LoopBegin = LoopStart;
            sBuffer.LoopLength = 0; // fuck. 

            svoice = new SourceVoice(SharpSLEngine.GetEngineBase(), fmt, VoiceFlags.None, 1024f);
           
            myMixer = SharpSLEngine.GetDefaultMixer(); 
            svoice.SetOutputVoices(new VoiceSendDescriptor(myMixer));            

          

          
       
        
        }



        public SoundEffect(string file,bool loop,int ls, int le )
        {
            isLooped = loop;
            LoopStart = ls;
            LoopEnd = le;
            // Console.WriteLine("{0} {1} {2} {3}", file, loop, ls, le);
            int channels, bits_per_sample, sample_rate;
            bufferData = SharpSLEngine.LoadWavFromFile(file, out channels, out bits_per_sample, out sample_rate);

            fmt = new WaveFormat(sample_rate, bits_per_sample, channels);
            sBuffer = new AudioBuffer();
            sBuffer.AudioBytes = bufferData.Length;
            sBuffer.Stream = new SharpDX.DataStream(bufferData.Length, true, true);
            sBuffer.Stream.Write(bufferData, 0, bufferData.Length);
            sBuffer.PlayBegin = 0;
            sBuffer.PlayLength = 0;

            var ll = le - ls;
            sBuffer.LoopCount = (loop ? 255 : 0);
            Console.WriteLine("==LoopStart== {0} -- {1} -- {2} / {3}", ls,le , ll, bufferData.Length) ;
           // Console.ReadLine();
            sBuffer.LoopBegin = ls;
            sBuffer.LoopLength = 0; // fuck. 

            svoice = new SourceVoice(SharpSLEngine.GetEngineBase(), fmt, VoiceFlags.None, 1024f);

            myMixer = SharpSLEngine.GetDefaultMixer();
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

        public SoundEffectInstance CreateInstance()
        {
            return new SoundEffectInstance(sBuffer,fmt,myMixer);
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
            // svoice.Dispose();
          
        }

        ~SoundEffect()
        {
            Dispose();
        }
    }
}
