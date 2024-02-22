using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xayrga.JAIDSP
{
    internal class JAIDSPEnvelope
    {
        public enum Mode
        {
            LINEAR = 0,
            SQUARE = 1, 
            SQUAREROOT = 2,
            CUBIC = 3,

            ENVELOPE_LOOP = 0x0D, 
            ENVELOPE_HOLD = 0x0E,
            ENVELOPE_STOP = 0x0F,            
        }

        public class Point
        {
            public Mode Mode;
            public short Duration;
            public short Value; 
        }

        public short Value;
        public float fValue;

        private short lastDuration = 0;
        private short currentDuration = 0;
        private int valueDelta = 0;
        private short lastValue = 0;

        private Point[] Vectors;
        private short currentVectorIndex = -1;
        private Mode currentMode;


        private void swapNextVector()
        {

            if (++currentVectorIndex >= Vectors.Length)
                throw new IndexOutOfRangeException("DSPEnvelope vector index exceeded vector list boundary!");

            var eVector = Vectors[currentVectorIndex];

            // Init duration
            lastDuration = currentDuration = eVector.Duration;
            currentMode = eVector.Mode;

            // STOP, LOOP, HOLD vectormodes don't have value
            if ((short)eVector.Mode < 0xA)
            {
                var envVal = eVector.Value;
                valueDelta = envVal - lastValue;
                lastValue = Value;            
            }      
        }

        public bool update()
        {
            if (currentMode == Mode.ENVELOPE_HOLD)
                return false; 
            else if (currentMode == Mode.ENVELOPE_STOP)
                return true;

            var deltaDepth = lastDuration - currentDuration;
            var fdeltaDepth = lastDuration / deltaDepth;

            switch (currentMode)
            {
                case Mode.LINEAR:
                    Value = (short)(lastValue + (valueDelta * fdeltaDepth));
                    break;
                case Mode.SQUARE:
                    Value = (short)(lastValue + valueDelta * Math.Pow(fdeltaDepth , 2));
                    break;
                case Mode.CUBIC:
                    Value = (short)(lastValue + valueDelta * Math.Pow(fdeltaDepth, 3));
                    break;
                case Mode.SQUAREROOT:
                    Value = (short)(lastValue + valueDelta * Math.Sqrt(fdeltaDepth));
                    break;
            }

            fValue = (float)Value / 0x7FFF;
            fValue*=fValue;

            if (currentDuration > 0)
                currentDuration--;
            else
                swapNextVector();

            return false;
        }

    }
}
