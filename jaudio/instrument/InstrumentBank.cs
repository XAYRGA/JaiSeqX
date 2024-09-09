using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jaudio.instrument
{
    public enum EJInstrumentEffectTarget
    {
        VOLUME = 0, 
        PITCH = 1, 
        PAN = 2,
        DOLBY = 3, 
        REVERB = 4,
    }

    public class JInstrumentBank
    {
        public int Size;
        public int ID;

        internal Dictionary<int, JInstrumentOscillator> Oscillators = new();
        internal Dictionary<int, JInstrumentSenseEffect> SenseEffects = new();
        internal Dictionary<int, JInstrumentRandEffect> RandEffects = new();
        internal Dictionary<int, JInstrument> Instruments = new();
        internal Dictionary<int, JEnvelopeVector[]> Envelopes = new();

        public JInstrumentRandEffect getRandEffect(int id)
        {
            if (RandEffects.ContainsKey(id))
                return RandEffects[id];
            return null;
        }

        public JInstrumentSenseEffect getSenseEffect(int id)
        {
            if (SenseEffects.ContainsKey(id))
                return SenseEffects[id];
            return null;
        }

        public JInstrumentOscillator getOscillator(int id)
        {
            if (Oscillators.ContainsKey(id)) 
                return Oscillators[id];
            return null;
        }

        public JInstrument getInstrument(int id)
        {
            if (Instruments.ContainsKey(id)) 
                return Instruments[id];
            return null;
        }
    }


    public class JInstrument
    {
        public float Pitch = 1;
        public float Volume = 1;
        public bool Percussion = false;

        public List<int> RandEffects = new();
        public List<int> SenseEffects = new();
        public List<int> Oscillators = new();
        public List<JKeyRegion> Keys = new();

        public class JVelocityRegion
        {
            public byte Velocity;
            public ushort WSYSID;
            public ushort WaveID;
            public float Volume;
            public float Pitch;
  
        }

        public class JKeyRegion
        {
            public byte BaseKey;
            public List<JVelocityRegion> Velocities = new();

            public float Volume = 1f;
            public float Pitch = 1f;

            public bool Percussion;
            public byte Pan = 64;
            public byte Attack;
            public byte Release;

            public virtual JVelocityRegion getVelocity(int vel)
            {
                JVelocityRegion vReg = Velocities[0];
                for (int i = 0; i < Velocities.Count; i++)
                    if (Velocities[i].Velocity >= vel)
                        return Velocities[i];
                return vReg;
            }
        }

        public virtual JKeyRegion getKey(int key)
        {
            JKeyRegion kReg = Keys[0];
            for (int i = 0; i < Keys.Count; i++)
                if (Keys[i].BaseKey >= key)
                    return Keys[i];
            return kReg;
        }


        public virtual JVelocityRegion getKeyVelocity(int key, int velocity)
        {
            JKeyRegion kReg = Keys[0];
            for (int i = 0; i < Keys.Count; i++)
                if (Keys[i].BaseKey >= key)
                    kReg = Keys[i];

            return kReg.getVelocity(velocity);
        }
    }

    public class JPercussionInstrument : JInstrument
    {
        public JPercussionInstrument()
        {
            Percussion = true;
        }
    }


    public class JInstrumentRandEffect 
    {
        public EJInstrumentEffectTarget Target;
        public float Floor;
        public float Ceiling;
    }

    public class JInstrumentSenseEffect
    {
        public EJInstrumentEffectTarget Target;
        public ESenseEffectTrigger Trigger;
        public byte Key;
        public float Floor;
        public float Ceiling;

        public enum ESenseEffectTrigger
        {
            Any = 0,
            Note = 1, 
            Velocity = 2,
        }
    }

    public class JInstrumentOscillator
    {
       /*
        ==LINEAR CURVE        
        .:*:.                                   
           :+=..                                
            ..=*.                               
                :*-.                            
                  :=+:.                         
                    .:#-                        
                        =+:.                    
                         .:+*.                  
                            .:#-.               
                               .=+-.            
                                 .:+*.          
                                    .-*-.       
                                       .=+-.    
                                         ..*+.  
                                            .-+-
       */
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


        /*
        ==ROOT CURVE
              .                                       
                *.                                      
                ==.                                     
                .+-                                     
                 .+:                                    
                  .*-                                   
                   .-*.                                 
                     .+-.                               
                       :+=.                             
                         .+*..                          
                            :+=.                        
                             ..:+*:                     
                                  .**-..                
                                    ..-++=..            
                                        ..:=%+:.        
                                              .:=*+=--::
        */

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

        /*
        == SQUARE CURVE
        -=+*=:.                                 
              .+#*-...                          
                   .=++-..                      
                       .:+*:                    
                            =*-.                
                              .=+:.             
                                .:#-            
                                   .*=.         
                                    ..+-        
                                       :%.      
                                        .+-.    
                                         .==    
                                           -*.  
                                            :+. 
                                            .-+.
                                              -+
        */
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
        /*
        ==CELL CURVE 
        =++.                                    
           ++.                                  
            :+.                                 
            .-=.                                
             .+-                                
              .*:                               
               .#.                              
                :*.                             
                .-=.                            
                  -+.                           
                   :*:                          
                    .==.                        
                      .*=..                     
                        .-+=:                   
                          ...-##=..             
                                 ..-=+**++==----
        */
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

        public EJInstrumentEffectTarget Target;
        public float Rate;
        public int AttackEnvelope;
        public int ReleaseEnvelope;
        public float Width;
        public float Base;

    }


    public class JEnvelopeVector
    {
        public JEnvelopeVectorMode Mode;
        public short Duration;
        public short Value;

        public enum JEnvelopeVectorMode
        {
            Linear = 0,
            Square = 1,
            SquareRoot = 2,
            SampleCell = 3,

            Loop = 0xD,
            Hold = 0xE,
            Stop = 0xF,
        }
    }

}
