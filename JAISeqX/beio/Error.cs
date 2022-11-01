using System;
using System.Collections.Generic;
using System.IO;

namespace Be.IO.Helpers
{
    internal static class Error
    {
        public static void Disposed()
        {
            throw new ObjectDisposedException(null);
        }

        public static void Disposed(object o, string message = null)
        {
            throw new ObjectDisposedException(o.GetType().Name, message);
        }

        public static void EndOfStream(string message = null)
        {
            throw new EndOfStreamException(message);
        }

        public static void Range()
        {
            throw new ArgumentOutOfRangeException();
        }

        public static void Range(string name, string message = null)
        {
            throw new ArgumentOutOfRangeException(name, message);
        }
    }
}
