using libJAudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace xayrga.JAIDSP
{
    internal class JAIDSPEnvelope
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
        private JEnvelopeVectorMode currentMode;

        public JAIDSPEnvelope(JEnvelopeVector[] env, short init, bool dbg = false)
        {
            Value = init;
            fValue = (float)Value / 0x7FFF;
            Vectors = env;

            debug = dbg;
            swapNextVector();
        }
        public static readonly float[] CURVE_LINEAR = {
            1.0f,
            0.9375f,
            0.875f,
            0.8125f,
            0.75f,
            0.6875f,
            0.625f,
            0.5625f,
            0.5f,
            0.4375f,
            0.375f,
            0.3125f,
            0.25f,
            0.1875f,
            0.125f,
            0.0625f,
            0
        };

        public static readonly float[] CURVE_SQUAREROOT = {
            1.0f,
            0.878906f,
            0.765625f,
            0.660156f,
            0.5625f,
            0.472656f,
            0.390625f,
            0.316406f,
            0.25f,
            0.191406f,
            0.140625f,
            0.097656f,
            0.0625f,
            0.0351562f,
            0.015625f,
            0.00390625f,
            0
        };

        public static readonly float[] CURVE_SQUARE = {
            1.0f,
            0.96824598f,
            0.935414f,
            0.90138799f,
            0.86602497f,
            0.82915598f,
            0.790569f,
            0.75f,
            0.707107f,
            0.66143799f,
            0.61237198f,
            0.559017f,
            0.5f,
            0.43301299f,
            0.353553f,
            0.25f,
            0
        };

        public static readonly float[] CURVE_SAMPLECELL = {
            1.0f,
            0.970489f,
            0.781274f,
            0.54628098f,
            0.39979199f,
            0.28931499f,
            0.21210399f,
            0.15747599f,
            0.112613f,
            0.081789598f,
            0.0579852f,
            0.0436415f,
            0.0308237f,
            0.0237129f,
            0.0152593f,
            0.00915555f,
            0
        };

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
            lastDuration = currentDuration = eVector.time;
            currentMode = eVector.mode;

            if ((short)eVector.mode < 0xA) // < 0xA is from the disasm
            {
                var envVal = eVector.value;
                valueDelta = envVal - Value;
                lastValue = Value;

                if (eVector.time == 0)
                {
                    Value = eVector.value;
                    fValue = (float)Value / 0x7FFF;
                    swapNextVector();
                }
            }
        }

        public unsafe bool update(double ms)
        {

            if (currentMode == JEnvelopeVectorMode.Hold)
                return false;
            else if (currentMode == JEnvelopeVectorMode.Stop)
                return true;
            else if (currentMode == JEnvelopeVectorMode.Loop)
            {
                var eVector = Vectors[currentVectorIndex];
                currentVectorIndex = (short)(eVector.value - 1);
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
                case JEnvelopeVectorMode.Square:
                    table = CURVE_SQUARE;
                    break;
                case JEnvelopeVectorMode.SampleCell:
                    table = CURVE_SAMPLECELL;
                    break;
                case JEnvelopeVectorMode.SqRoot:
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
