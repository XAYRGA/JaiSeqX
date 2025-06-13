using jaudio.instrument;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Un4seen.Bass;
using xayrga.JAIDSP;

namespace JAISeqX.jdsp
{
    internal enum VoiceMatrixSlot
    {
        BASE, 
        BEND,
        PARAM,
        OSCILLATOR
    }
    internal class Voice
    {
        private enum VoiceState
        {
            SIMPLE_ATTACK,
            SIMPLE_RELEASE,
            IDLE,
            OSCILLATOR_ATTACK,
            OSCILLATOR_RELEASE
        }

        public enum VoiceOperation
        {
            KEEP,
            DESTROY
        }

        private float[] pitchMatrix = { 1f, 1f, 1f, 1f };
        private float[] gainMatrix = { 1f, 1f, 1f, 1f };
        private float[] panMatrix = { 64, 64, 64, 64 };
         
        Envelope Envelope;
        JInstrumentOscillator Oscillator; 

        private int voiceHandle;
        private int syncHandle;
        private int fxHandle;

        private double FadeTime = 0;
        private double TotalFadeTime = 0;

        SoundBuffer buffer;

        VoiceState State;

        public Voice(ref SoundBuffer bufferRef)
        {
            buffer = bufferRef;  // save root buffer.
            BASSFlag Flags = 0;

            if (!buffer.Loop.Enable)
                voiceHandle = Bass.BASS_StreamCreateFile(buffer.BufferHandle, 0, buffer.SampleCount * 2, BASSFlag.BASS_DEFAULT);
            else
            {
                voiceHandle = Bass.BASS_StreamCreateFile(buffer.BufferHandle, 0, buffer.SampleCount * 2, BASSFlag.BASS_DEFAULT);
                Bass.BASS_ChannelSetPosition(voiceHandle, buffer.Loop.Start * 2, BASSMode.BASS_POS_LOOP);
                Bass.BASS_ChannelSetPosition(voiceHandle, buffer.Loop.End * 2, BASSMode.BASS_POS_END);
            }
        }


        public float getPitchMatrix(byte index)
        {
            return pitchMatrix[index];
        }

        public void setVolumeMatrix(VoiceMatrixSlot slot, float volume)
        {
            gainMatrix[(byte)slot] = volume;
            recalculateParameters();
        }

        private void recalculateParameters(bool dont = false)
        {
            if (dont)
                return;

            float pv = 1f;
            for (int i = 0; i < pitchMatrix.Length; i++)
                pv *= pitchMatrix[i];
            Bass.BASS_ChannelSetAttribute(voiceHandle, BASSAttribute.BASS_ATTRIB_FREQ, buffer.SampleRate * pv);

            float vv = 1f;
            for (int i = 0; i < gainMatrix.Length; i++)
                vv *= gainMatrix[i];
            Bass.BASS_ChannelSetAttribute(voiceHandle, BASSAttribute.BASS_ATTRIB_VOL, vv);

            var panValue = 1f;
            for (int i = 0; i < panMatrix.Length; i++)
                panValue *= ((panMatrix[i]) / 64f);
            panValue = (float)Math.Pow(panValue, 2);

            Bass.BASS_ChannelSetAttribute(voiceHandle, BASSAttribute.BASS_ATTRIB_PAN, (panValue - 1f));
        }

        public void destroy()
        {

        }

        public VoiceOperation updateVoice(double delta)
        {
            switch(State)
            {
                case VoiceState.OSCILLATOR_ATTACK:
                case VoiceState.OSCILLATOR_RELEASE:
                    if (Envelope == null || Envelope.update(delta))
                    {
                        destroy();
                        return VoiceOperation.DESTROY;
                    }
                    setVolumeMatrix(VoiceMatrixSlot.OSCILLATOR, Envelope.fValue);
                    break;
                case VoiceState.SIMPLE_ATTACK:
                    FadeTime -= delta;
                    setVolumeMatrix(
                        VoiceMatrixSlot.OSCILLATOR,
                        (float)Math.Clamp((TotalFadeTime - FadeTime) / TotalFadeTime, 0, 1)
                    );
                    break;
                case VoiceState.SIMPLE_RELEASE:
                    FadeTime -= delta;
                    setVolumeMatrix(
                        VoiceMatrixSlot.OSCILLATOR,
                        1f - (float)Math.Clamp((TotalFadeTime - FadeTime) / TotalFadeTime, 0, 1)
                    );
                    if (FadeTime <= 0)
                        return VoiceOperation.DESTROY;
                    break;

            }
            recalculateParameters();    
            return VoiceOperation.KEEP;
        }



    }
}
