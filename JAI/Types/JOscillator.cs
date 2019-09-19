using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JaiSeqX.JAI.Types
{
    public class JOscillator
    {
        public static JOscillatorVector zeroVector = new JOscillatorVector { mode = JOscillatorVectorMode.Linear, time = 0, value = 0 };
        public JOscillatorTarget target;
        public float rate;
        public JOscillatorVector[] ASVector;
        public JOscillatorVector[] DRVector;
        public float Width;
        public float Vertex;

        private byte state;
        private JOscillatorVector[] currentVectorSet;

        private JOscillatorVector lastVector;
        private JOscillatorVector currentVector;

        private float position;
        private float duration;

        private short vectorPosition;

        public void advance()
        {
            if (state == 1 )
            {
                position += rate; // advance at the rate of the oscillator
                duration += rate; // same with the duration
                /*
                Console.Write(currentVector.mode);
                Console.Write(" ");
                Console.WriteLine(" {0} {1} {2}", position, duration, currentVector.time);
                */
                if (currentVector.time <= position) // check if the position of the vector has exceeded our tie
                {
                    vectorPosition++; // if it did we need to advance to the next vector
                    duration = 0; // and reset the duration
                    if (vectorPosition >= currentVectorSet.Length) // make sure when we grab our vector that we're not exceeding our envelope
                    {
                        state = 0; // if we are, we need to turn the oscillator off
                        return; // we can stop here
                    }
                    var nextVector = currentVectorSet[vectorPosition]; // store the next vector
                    switch (nextVector.mode) // check if it's a special vector
                    {
                        // if it matches anything in this case, it's a special vector
                        case JOscillatorVectorMode.Loop: // loop tells us to:
                            {
                                vectorPosition = 0; // go back to the beginning of this vector map
                                position = 0; // then reset our time so all of the events play again
                                break; // then we can drop out of this switch case.
                            }
                        case JOscillatorVectorMode.Hold: // if it is a hold
                            {
                                //Console.WriteLine("OSCILLATOR TRIGGER HOLD");
                                state = 2; // State 2 tells us to keep the oscillators value, but stop oscillating.
                                /* Will keep this  commmented for now -- need to do some testing to see how it sounds. 
                                // Because this will continue, we'll need to keep the previous vector data :) 
                                vectorPosition++; // so we advance the vector one more timem
                                duration = 0; // and reset the duration
                                if (vectorPosition >= currentVectorSet.Length) // make sure wh are not exceeding the vector map size again
                                {
                                    state = 0; // if we are, fuck it. Turn off
                                    return; // then return. 
                                }
                                lastVector = currentVector; // Swap the vector for the time difference --  (Time difference might be fucked up because hold operation doesn't end at the beginning of the other?)
                                currentVector = currentVectorSet[vectorPosition]; // then load the new vector
                                */

                                return; // since we're not doing any additional oscillation, we should just stop -- we should continue when the instrument stops. 
                            }
                        case JOscillatorVectorMode.Stop: // Stop tells us to stop completely, and reset the instrument -- changes are discarded.
                            {
                                // find a way to reset value?
                                state = 3; // State 3
                                return;
                            }
                    }
                    lastVector = currentVector; // Swap the vector for the time difference
                    currentVector = currentVectorSet[vectorPosition]; // then load   the new vector
                    if (currentVector.time < position) // error if nintendo did a dumb -- and the sorting algorithm didn't catch it.
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("FUCK: ");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine("New oscillator vector position is less than previous vector.");
                    }
                }
            } 
        }

        public void attack()
        {
            if (ASVector==null) { state = 0; return; }
            lastVector = zeroVector;
            currentVectorSet = ASVector;
            currentVector = currentVectorSet[0];
            position = 0;
            duration = 0;
            vectorPosition = 0;
            state = 1;
        }

        public void release()
        {
            if (DRVector == null) { state = 0; return; }
            if (lastVector==null)
            {
                lastVector = zeroVector; 
            }
            currentVectorSet = DRVector;
            currentVector = currentVectorSet[0];
            position = 0;
            duration = 0;
            vectorPosition = 0;
            state = 1;
        }
    }

    public class JOscillatorVector
    {
        public JOscillatorVectorMode mode;
        public short time;
        public short value;
    }

    public enum JOscillatorVectorMode
    {
        Linear = 0,
        Square = 1,
        SquareRoot = 2,
        SampleCell = 3,

        Loop = 13,
        Hold = 14,
        Stop = 15,
    }

    public enum JOscillatorTarget
    {
        Volume = 1,
        Pitch = 2,
        Pan = 3,
        FX  = 4,
        Dolby = 5
    }
}
