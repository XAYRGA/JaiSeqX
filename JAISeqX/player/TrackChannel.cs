using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using jaudio.instrument;
using jdsp;

namespace JAISeqX.player
{
    internal class TrackChannel
    {
        SequencePlayer _player;
        LinearSlide Pitch  = new jdsp.LinearSlide();
        LinearSlide Volume = new jdsp.LinearSlide();
        LinearSlide Pan = new jdsp.LinearSlide();
        Modulator Vibrato = new jdsp.Modulator(ModulatorType.SINE) { Frequency = 6, Depth = 1};

        Voice[] Voices = new Voice[7];
        List<Voice> VoiceOrphans = new List<Voice>();    

        JInstrument Instrument;
        JInstrumentBank InstrumentBank;


        public TrackChannel(SequencePlayer player)
        {
            _player = player;
            Pan.setTarget(64, 0);
            Pitch.setTarget(1, 0);
            Volume.setTarget(1, 0);
        }

        public void selectInstrument(int instrument)
        {
            Instrument = InstrumentBank?.getInstrument(instrument);
        }

        public void selectBank(int bankId)
        {
            if (_player.Instruments.ContainsKey(bankId)) 
                InstrumentBank = _player.Instruments[bankId];
        }

        public void voiceOff(int voiceId)
        {
            if (voiceId > Voices.Length)
                return; 
            var voice = Voices[voiceId];   
            if (voice == null) return;

            voice.Stop();

            Voices[voiceId] = null;
        }

        public void noteOn(int note, int voiceid, int velocity)
        {
            if (Instrument == null)
                return;
            var keyRegion = Instrument.getKey(note);
            if (keyRegion == null) 
                return;
            var velRegion  = keyRegion.getVelocity(voiceid);
            if (velRegion == null) 
                return;
            // todo: this is still a bit gross :( 
            if (!_player.Waves.ContainsKey(velRegion.WSYSID))
                return;
            if (!_player.Waves[velRegion.WSYSID].Waves.ContainsKey(velRegion.WaveID))
                return;

            var waveInfo = _player.Waves[velRegion.WSYSID].Waves[velRegion.WaveID];
            var soundBuffer = _player.WaveSounds[velRegion.WSYSID][velRegion.WaveID];

            var voice = new jdsp.Voice(ref soundBuffer);

            var pitchCalc = keyRegion.Pitch * velRegion.Pitch;
            var volumeCalc = keyRegion.Volume * velRegion.Volume;

            // calculate note log
            pitchCalc *= (float)Math.Pow(2f, (note - waveInfo.Key) / 12f);

            // calculate velocity (exponential) 
            volumeCalc *= (float)Math.Pow(2f, (velocity / 127));

            voice.setPitchMatrix(VoiceMatrixSlot.BASE, pitchCalc);
            voice.setVolumeMatrix(VoiceMatrixSlot.BASE, volumeCalc);

            voice.setPitchMatrix(VoiceMatrixSlot.BEND, Pitch.fValue);
            voice.setVolumeMatrix(VoiceMatrixSlot.BEND,Volume.fValue);

            voice.setPitchMatrix(VoiceMatrixSlot.PARAM, Vibrato.Value);

            Voices[voiceid] = voice;
            if (Instrument.Oscillators.Count > 0)
                voice.setOscillator(InstrumentBank.getOscillator(Instrument.Oscillators[0]));
        }


        public void tick()
        {
            Pitch.update();
            Volume.update();
            Pan.update();
        }

        public void setPitch(int amount, int ticks = 0)
        {
            Pitch.setTarget(amount, ticks);
        }

        public void setVolume(int volume, int ticks = 0)
        {
            Volume.setTarget(volume, ticks);
        }

        public void setPan(int pan, int ticks = 0)
        {
            Pan.setTarget(pan, ticks);
        }

        public void updateDSP(double deltatime)
        {
            Vibrato.update(deltatime, ModulatorType.SINE);

            // Update order is important! 
            // If you update the orphans AFTER you update the voices
            // It will update released voices twice
            // resulting in cascading milisecond inaccuracy!
            for (int i = 0; i < VoiceOrphans.Count; i++)
                if (VoiceOrphans[i] != null)
                {
                    var orphan = VoiceOrphans[i];
                    if (orphan.updateVoice(deltatime) == Voice.VoiceOperation.DESTROY)
                        VoiceOrphans.Remove(orphan);

                    orphan.setPitchMatrix(VoiceMatrixSlot.BEND, Pitch.fValue);
                    orphan.setPitchMatrix(VoiceMatrixSlot.PARAM, Vibrato.Value);
                }

            for (int i = 0; i < Voices.Length; i++)
            {
                var voice = Voices[i];
                if (voice == null)
                    continue;

                if (voice.updateVoice(deltatime) == Voice.VoiceOperation.RELEASE)
                {
                    VoiceOrphans.Add(voice);
                    Voices[i] = null;
                    continue; 
                }
                voice.setPitchMatrix(VoiceMatrixSlot.BEND, Pitch.fValue);
                voice.setPitchMatrix(VoiceMatrixSlot.PARAM, Vibrato.Value);
            }
        }
    }
}
