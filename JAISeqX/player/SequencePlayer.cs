using jaudio;
using jaudio.instrument;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JAISeqX.player
{
    internal class SequencePlayer
    {
        public float Tempo;
        public float TicksPerBeat;
        public bool Pause;

        public SequenceTrack RootTrack;
        
        private float _tickLength = 0;
        private byte[] fileBuffer;
        private Stopwatch _tickTimer = new Stopwatch();
        private double _lastTick = 0;
        private double _tickCount = 0;

        internal Dictionary<int, JWaveSystem> Waves = new Dictionary<int, JWaveSystem>();    
        internal Dictionary<int, JInstrumentBank> Instruments = new Dictionary<int, JInstrumentBank>();
        internal Dictionary<int, Dictionary<int, jdsp.SoundBuffer>> WaveSounds = new();


        public SequencePlayer(
            ref byte[] Sequence,
            Dictionary<int, JWaveSystem> WaveSystems, 
            Dictionary<int, JInstrumentBank> InstrumentBanks, 
            Dictionary<int, Dictionary<int, jdsp.SoundBuffer>> SoundBuffers
        )
        {
            Waves = WaveSystems;
            Instruments = InstrumentBanks;
            WaveSounds = SoundBuffers;
            fileBuffer = Sequence;
        }


        public void recalculateTickLength()
        {
            _tickLength = (60000f / Tempo) / TicksPerBeat;
        }

        public void Play()
        {
            Pause = false; 
            _tickTimer.Start();
        }

        public void Update()
        {
            var deltaTime = _tickTimer.ElapsedMilliseconds - _lastTick;
            _lastTick = _tickTimer.ElapsedMilliseconds;
            // TODO: If there is a delay between multiple ticks,
            // it's possible that this will cause time inconsistencies,
            // since the same deltatime is reused.
            if (Pause)
                return;
            RootTrack.update(deltaTime);
            var prevTicks = _tickCount;
            _tickCount += (deltaTime / _tickLength);
            for (int i=0; i < (prevTicks - _tickCount); i++)
                RootTrack.tick();
        }

        public void SetSequence(byte[] seq)
        {
            fileBuffer = seq;
        }



       


    }
}
