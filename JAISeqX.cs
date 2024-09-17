using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xayrga.console;

namespace JaiSeqX
{
    internal static class Application
    {
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
            0,
        };

        public static void Main(string[] args)
        {
            ConsoleAppHelper.ArgumentList = args;

            var frac = 0f;
            while (true)
            {
                frac += 0.1f;
                Thread.Sleep(100);
                Console.WriteLine(InterpolateTable(CURVE_LINEAR,frac));
                if (frac >= 1)
                    frac = 0;
                
            }

        }

        public static float InterpolateTable(float[] curve, float depth)
        {
            if (depth > 1 )
                depth = 1;

            var real = depth * (curve.Length - 1);
            var integer = (int)Math.Floor(real);
            var frac = real - integer;

            var d1 = curve[integer];
            var d2 = 0f;
            if (integer < curve.Length -1) 
                d2 = curve[integer + 1];

            return d1 + (d2 - d1) * frac;
        }
    }
}
