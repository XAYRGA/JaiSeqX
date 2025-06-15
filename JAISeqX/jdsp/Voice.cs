using jaudio.instrument;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Un4seen.Bass;


namespace jdsp
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
            OSCILLATOR_RELEASE,
            KILL

        }

        public enum VoiceOperation
        {
            KEEP,
            RELEASE,
            DESTROY
        }

        private float[] pitchMatrix = { 1f, 1f, 1f, 1f };
        private float[] gainMatrix = { 1f, 1f, 1f, 1f };
        private float[] panMatrix = { 64, 64, 64, 64 };
         
        Envelope mEnvelope;
        JInstrumentOscillator Oscillator; 

        private int voiceHandle;
        private int loopHandle;

        private double FadeTime = 0;

        private double FadeIn = 0;
        private double FadeOut = 0;

        SoundBuffer buffer;

        VoiceState State;

        public Voice(ref SoundBuffer bufferRef)
        {
            buffer = bufferRef;  // save root buffer.

            if (!buffer.Loop.Enable)
                voiceHandle = Bass.BASS_StreamCreateFile(buffer.BufferHandle, 0, buffer.SampleCount * 2, BASSFlag.BASS_DEFAULT);
            else
            {
                var trueLoopStart = buffer.Loop.Start * 2;
                var trueLoopEnd = buffer.Loop.End * 2;
            
                voiceHandle = Bass.BASS_StreamCreateFile(buffer.BufferHandle, 0, buffer.HandleSize,BASSFlag.BASS_DEFAULT);
                loopHandle = Bass.BASS_ChannelSetSync(voiceHandle,
                        BASSSync.BASS_SYNC_POS | 
                        BASSSync.BASS_SYNC_MIXTIME | 
                        BASSSync.BASS_SYNC_THREAD |
                        BASSSync.BASS_SYNC_MIXTIME,
                    trueLoopEnd,
                    jdsp.loopProc,
                    new IntPtr(trueLoopStart)
                );

            }
        }

        public void setOscillator(JInstrumentOscillator oscillator)
        {
            Oscillator = oscillator;
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

        public void setPitchMatrix(VoiceMatrixSlot slot, float value)
        {
            pitchMatrix[(byte)slot] = value;
            recalculateParameters();
        }



        public void setPanMatrix(byte index, float pan)
        {
            panMatrix[index] = pan;
            recalculateParameters();
        }


        public void setAttackRelease(int attack, int release)
        {
            FadeIn = attack;
            FadeOut = release;
        }

        private void recalculateParameters()
        {

            if (State == VoiceState.KILL)
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


        public void Play()
        {
            if (Oscillator != null)
            {
                mEnvelope = new Envelope(Oscillator.AttackEnvelopeReference, 32767);
                State = VoiceState.OSCILLATOR_ATTACK;
            } else if (FadeIn + FadeOut > 0)
            {
                FadeTime = FadeIn;
                State = VoiceState.SIMPLE_ATTACK;
            }
                
            Bass.BASS_ChannelPlay(voiceHandle, true);
            recalculateParameters();
        }


        public void Stop()
        {
            switch(State)
            {
                case VoiceState.OSCILLATOR_ATTACK:
                    mEnvelope = new Envelope(Oscillator.ReleaseEnvelopeReference, mEnvelope.Value);
                    State = VoiceState.OSCILLATOR_RELEASE;
                    return;
                case VoiceState.IDLE:
                    destroy();
                    return;
                case VoiceState.SIMPLE_ATTACK:
                    FadeTime = FadeOut;
                    State = VoiceState.SIMPLE_RELEASE;
                    return;
            }
        }

        public void destroy()
        {
            State = VoiceState.KILL;
            Bass.BASS_ChannelStop(voiceHandle);
            Bass.BASS_ChannelFree(voiceHandle);
            
        }

        public VoiceOperation updateVoice(double delta)
        {
            switch(State)
            {
                case VoiceState.KILL:
                    return VoiceOperation.DESTROY;                    
                case VoiceState.OSCILLATOR_ATTACK:
                case VoiceState.OSCILLATOR_RELEASE:
                    if (mEnvelope == null || mEnvelope.update(delta * Oscillator.Rate))
                    {
                        destroy();
                        return VoiceOperation.DESTROY;
                    }
                    setVolumeMatrix(VoiceMatrixSlot.OSCILLATOR, mEnvelope.fValue);
                    break;
                case VoiceState.SIMPLE_ATTACK:
                    FadeTime -= delta;
                    setVolumeMatrix(
                        VoiceMatrixSlot.OSCILLATOR,
                        (float)Math.Clamp((FadeIn - FadeTime) / FadeIn, 0, 1)
                    );
                    break;
                case VoiceState.SIMPLE_RELEASE:
                    FadeTime -= delta;
                    setVolumeMatrix(
                        VoiceMatrixSlot.OSCILLATOR,
                        1f - (float)Math.Clamp((FadeOut - FadeTime) / FadeOut, 0, 1)
                    );
                    if (FadeTime <= 0)
                        return VoiceOperation.DESTROY;
                    break;

            }
            // already setting the volume every update which calls this.
            // so i guess no need to waste the cycles
            //recalculateParameters();  

            if (State == VoiceState.SIMPLE_RELEASE || State == VoiceState.OSCILLATOR_RELEASE)
                return VoiceOperation.RELEASE;

            return VoiceOperation.KEEP;
        }



    }
}
