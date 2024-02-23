using libJAudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xayrga.JAIDSP
{
    internal class JAIDSPEnvelopeTemp
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

        bool pv1 = false;
        bool pv2 = false;

        public JAIDSPEnvelopeTemp(JEnvelopeVector[] env, short init, bool dbg = false)
        {
            Value = init;
            fValue = (float)Value / 0x7FFF;
            Vectors = env;

            debug = dbg;
            swapNextVector();
        }


        private void swapNextVector()
        {

            if (++currentVectorIndex >= Vectors.Length)
                throw new IndexOutOfRangeException("DSPEnvelope vector index exceeded vector list boundary!");

            var eVector = Vectors[currentVectorIndex];

            // Init duration
            lastDuration = currentDuration = eVector.time;
            currentMode = eVector.mode;

            //Console.WriteLine(currentMode);
            // STOP, LOOP, HOLD vectormodes don't have value


            if ((short)eVector.mode < 0xA)
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

        public bool update(double ms)
        {

            if (currentMode == JEnvelopeVectorMode.Hold)
                return false; 
            else if (currentMode == JEnvelopeVectorMode.Stop)
                return true;


        
                var deltaDepth = lastDuration - currentDuration;

                var fdeltaDepth = deltaDepth / lastDuration;

                if (fdeltaDepth > 1)
                    fdeltaDepth = 1f;

               // if (debug)
                    //Console.WriteLine($"{deltaDepth}dd {fdeltaDepth} cd{currentDuration} ld{lastDuration} vd{valueDelta}");

                switch (currentMode)
                {
                    case JEnvelopeVectorMode.Linear:
                        Value = (short)(lastValue + (valueDelta * fdeltaDepth));
                        break;
                    case JEnvelopeVectorMode.Square:
                        Value = (short)(lastValue + valueDelta * Math.Pow(fdeltaDepth, 2));
                        break;
                    case JEnvelopeVectorMode.Cubic:
                        Value = (short)(lastValue + valueDelta * Math.Pow(fdeltaDepth, 3));
                        break;
                    case JEnvelopeVectorMode.SqRoot:
                        Value = (short)(lastValue + valueDelta * Math.Sqrt(fdeltaDepth));
                        break;
                }
            

            fValue = (float)Value / (float)0x7FFF;

            if (currentDuration > 0)
                currentDuration-=ms;
            else
                swapNextVector();

            return false;
        }

    }
}
