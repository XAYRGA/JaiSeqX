using System;
using System.Collections.Generic;

namespace Be.IO.Helpers
{
    internal static class Empty
    {
        public static T[] Array<T>() =>
            Container<T>.Array;

        private static class Container<T>
        {
            public static readonly T[] Array = new T[0];
        }
    }
}
