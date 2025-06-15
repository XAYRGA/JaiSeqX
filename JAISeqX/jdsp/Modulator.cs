using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jdsp
{
    public enum ModulatorType
    {
        SINE,
        SQUARE,
        TRI
    }
    internal class Modulator
    {
        public float Frequency;
        public float Depth;
        public float Pitch = 12; // Semitones!
        public float Value;

        private double _time;
        private ModulatorType _type;

        public Modulator(ModulatorType type) {
            _type = type;
        }

        public void update(double delta, ModulatorType type)
        {
            _time += delta;
            switch (_type)
            {
                case ModulatorType.SINE:
                    Value = (float)Math.Pow(2,(Math.Sin(Frequency * _time) * Depth) / Pitch);
                    break;
                case ModulatorType.SQUARE:
                    Value = (float)Math.Pow(2,((Math.Sin(Frequency * _time)) > 0 ? 1 * Depth : -1 * Depth) / Pitch);
                    break;
            }
        }
    }
}
