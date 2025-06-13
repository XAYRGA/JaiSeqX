using jaudio.instrument;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace xayrga.JAIDSP
{
    internal class Envelope
    {
        public short Value;
        public float fValue;

        private double lastDuration = 0;
        private double currentDuration = 0;
        private int valueDelta = 0;
        private short lastValue = 0;

        public bool debug = false;

        private JEnvelopeVector[] Vectors;
        private short currentVectorIndex = -1;
        private JEnvelopeVector.JEnvelopeVectorMode currentMode;

        public Envelope(JEnvelopeVector[] env, short init, bool dbg = false)
        {
            Value = init;
            fValue = (float)Value / 0x7FFF;
            Vectors = env;

            debug = dbg;
            swapNextVector();
        }
        public static readonly float[] CURVE_LINEAR = JInstrumentOscillator.CURVE_LINEAR;

        public static readonly float[] CURVE_SQUAREROOT = JInstrumentOscillator.CURVE_SQUAREROOT;

        public static readonly float[] CURVE_SQUARE = JInstrumentOscillator.CURVE_SQUARE;

        public static readonly float[] CURVE_SAMPLECELL = JInstrumentOscillator.CURVE_SAMPLECELL;

        public static float InterpolateTable(float[] curve, float depth)
        {
            if (depth > 1)
                depth = 1;

            var real = depth * (curve.Length - 1);
            var integer = (int)Math.Floor(real);
            var frac = real - integer;

            var d1 = curve[integer];
            var d2 = 0f;
            if (integer < curve.Length - 1)
                d2 = curve[integer + 1];

            return d1 + (d2 - d1) * frac;
        }

        public static float InterpolateTableInverse(float[] curve, float depth)
        {
            return 1f - InterpolateTable(curve, depth);
        }
        private void swapNextVector()
        {

            if (++currentVectorIndex >= Vectors.Length)
                throw new Exception($"Enveloped exited vector list boundary!");


            var eVector = Vectors[currentVectorIndex];

            // Init duration
            lastDuration = currentDuration = eVector.Duration;
            currentMode = eVector.Mode;

            if ((short)eVector.Mode < 0xA) // < 0xA is from the disasm
            {
                var envVal = eVector.Value;
                valueDelta = envVal - Value;
                lastValue = Value;

                if (eVector.Duration == 0)
                {
                    Value = eVector.Duration;
                    fValue = (float)Value / 0x7FFF;
                    swapNextVector();
                }
            }
        }

        public unsafe bool update(double ms)
        {

            if (currentMode == JEnvelopeVector.JEnvelopeVectorMode.Hold)
                return false;
            else if (currentMode == JEnvelopeVector.JEnvelopeVectorMode.Stop)
                return true;
            else if (currentMode == JEnvelopeVector.JEnvelopeVectorMode.Loop)
            {
                var eVector = Vectors[currentVectorIndex];
                currentVectorIndex = (short)(eVector.Value - 1);
                swapNextVector();
                return false;
            }

            var deltaDepth = lastDuration - currentDuration;
            var fdeltaDepth = (float)(deltaDepth / lastDuration);

            if (fdeltaDepth > 1)
                fdeltaDepth = 1f;
            var table = CURVE_LINEAR;

            switch (currentMode)
            {
                case JEnvelopeVector.JEnvelopeVectorMode.Square:
                    table = CURVE_SQUARE;
                    break;
                case JEnvelopeVector.JEnvelopeVectorMode.SampleCell:
                    table = CURVE_SAMPLECELL;
                    break;
                case JEnvelopeVector.JEnvelopeVectorMode.SquareRoot:
                    table = CURVE_SQUAREROOT;
                    break;
            }

            Value = (short)(lastValue + valueDelta * InterpolateTableInverse(table, fdeltaDepth));
            fValue = Value / 32767f;

            if (currentDuration > 0)
                currentDuration -= ms;
            else
                swapNextVector();

            return false;
        }

    }
}
