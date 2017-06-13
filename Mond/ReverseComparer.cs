using System;
using System.Collections.Generic;

namespace Mond
{
    class ReverseComparer<T> : IComparer<T> where T : IComparable<T>
    {
        public static IComparer<T> Instance { get; } = new ReverseComparer<T>();

        public int Compare(T x, T y)
        {
            return y.CompareTo(x);
        }
    }
}
