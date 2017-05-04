using System;
using System.Collections.Generic;

namespace Mond
{
    class GenericComparer<T> : IComparer<T>
    {
        private readonly Func<T, T, int> _comparer;

        public GenericComparer(Func<T, T, int> comparer)
        {
            _comparer = comparer;
        }

        public int Compare(T x, T y)
        {
            return _comparer(x, y);
        }
    }
}
