using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace jdsp
{
    internal class LinearSlide
    {
        float lastValue = 0;
        float valueDelta = 0;
        float currentValue = 0;
        float duration = 0;
        float targetDuration = 0;
        public int Value { get => (int)currentValue; }
        public float fValue { get => currentValue; }

        public LinearSlide(float init = 0)
        {
            currentValue = init;
            lastValue = init;
        }
        public void setTarget(float value, int durationTicks)
        {
            if (durationTicks == 0)
            {
                duration = 0;
                targetDuration = 0;
                currentValue = value;
                lastValue = currentValue;
                return;
            }
            valueDelta = value - currentValue;
            lastValue = currentValue;
            duration = 1; // I guess we start at 1?
            targetDuration = durationTicks;
        }

        public void update()
        {       
            if (duration >= targetDuration)
                return;
            duration++;
            var durationCoefficient = (duration / targetDuration);
            currentValue = (int)(lastValue + (valueDelta * durationCoefficient));              
        }        
    }
}
