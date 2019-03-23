using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.XAPO.Fx;
using SharpDX.Multimedia; 
using SharpDX.XAudio2;
using System.IO;

namespace XAYRGA.SharpSL
{
    public static class SharpSLEngine
    {
        private static XAudio2 RootContext;
        public static MasteringVoice FOutput; 

        private static bool inited;
        public static bool Init()
        {
            if (!inited)
            {
                RootContext = new XAudio2();
                FOutput = new MasteringVoice(RootContext);
                inited = true;


           


            } else
            {
                throw new Exception("Already initialized.");
            }
            
            return true; 

        }

        public static byte[] LoadWave(Stream stream, out int channels, out int bits, out int rate)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            using (BinaryReader reader = new BinaryReader(stream))
            {
                // RIFF header
                string signature = new string(reader.ReadChars(4));
                if (signature != "RIFF")
                    throw new NotSupportedException("Specified stream is not a wave file.");

                int riff_chunck_size = reader.ReadInt32();

                string format = new string(reader.ReadChars(4));
                if (format != "WAVE")
                    throw new NotSupportedException("Specified stream is not a wave file.");

                // WAVE header
                string format_signature = new string(reader.ReadChars(4));
                if (format_signature != "fmt ")
                    throw new NotSupportedException("Specified wave file is not supported.");

                int format_chunk_size = reader.ReadInt32();
                int audio_format = reader.ReadInt16();
                int num_channels = reader.ReadInt16();
                int sample_rate = reader.ReadInt32();
                int byte_rate = reader.ReadInt32();
                int block_align = reader.ReadInt16();
                int bits_per_sample = reader.ReadInt16();

                string data_signature = new string(reader.ReadChars(4));
                if (data_signature != "data")
                    throw new NotSupportedException("Specified wave file is not supported.");

                int data_chunk_size = reader.ReadInt32();

                channels = num_channels;
                bits = bits_per_sample;
                rate = sample_rate;

                return reader.ReadBytes((int)reader.BaseStream.Length);
            }
        }
        public static SubmixVoice GetDefaultMixer()
        {
            var Mixer = new SubmixVoice(RootContext);
            /*
            Echo effect = new Echo(RootContext);


            EffectDescriptor effectDescriptor = new EffectDescriptor(effect)
            {
                InitialState = true,
                OutputChannelCount = 2
            };
            Mixer.SetEffectChain(effectDescriptor);
           
            EchoParameters effectParameter = new EchoParameters
            {
                Delay = 160f,
                Feedback = 0f,
                WetDryMix = 0f
            };
            Mixer.SetEffectParameters(0, effectParameter);
            
            Mixer.EnableEffect(0);
            */
            return Mixer;
        }
        public static XAudio2 GetEngineBase()
        {
            return RootContext;
        }
        public static byte[] LoadWavFromFile(string fnam, out int channels, out int bits, out int rate)
        {
            return LoadWave(File.OpenRead(fnam), out channels, out bits, out rate);
        }


    }
}
