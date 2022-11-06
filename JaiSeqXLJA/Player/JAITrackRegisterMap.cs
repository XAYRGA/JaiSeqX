using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JaiSeqXLJA.Player
{
   public  class JAITrackRegisterMap
    {
        Dictionary<byte,short> InternalDict;
        public byte[] changed; 
        public JAITrackRegisterMap()
        {
            InternalDict = new Dictionary<byte, short>(255);
            changed = new byte[255];
        }
        public short this[byte index]
        {
            get
            {
                short tgi = 0;
                InternalDict.TryGetValue(index, out tgi);
                return tgi;
            }
            set
            {
                InternalDict[index] = value;
                changed[index] = 100;
            }
        }

        public void clearChanged()
        {
            for (int i=0; i < changed.Length;i++)
            {
                changed[i] = 100;
            }
        }
        ~JAITrackRegisterMap()
        {
            
        }
    }
}
