using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Be.IO;

namespace jaudio
{
    public abstract class JAudioSerializable
    {
        public int mBaseAddress;
        public abstract void WriteToStream(BeBinaryWriter writer);
    }
}
