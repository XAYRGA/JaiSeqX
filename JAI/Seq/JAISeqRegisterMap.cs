using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JaiSeqX.JAI.Seq
{
   public  class JAISeqRegisterMap
    {
        Dictionary<byte,short> InternalDict;

        public JAISeqRegisterMap()
        {
            InternalDict = new Dictionary<byte, short>(255);
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
            }
        }

        ~JAISeqRegisterMap()
        {
            
        }
    }
}
