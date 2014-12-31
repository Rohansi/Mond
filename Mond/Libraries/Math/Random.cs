using System;
using Mond.Binding;

namespace Mond.Libraries.Math
{
    [MondClass("Random")]
    internal class MondRandom
    {
        private readonly Random _random;

        [MondConstructor]
        public MondRandom()
        {
            _random = new Random();
        }

        [MondConstructor]
        public MondRandom(int seed)
        {
            _random = new Random(seed);
        }

        [MondFunction("next")]
        public int Next()
        {
            return _random.Next();
        }

        [MondFunction("next")]
        public int Next(int maxValue)
        {
            return _random.Next(maxValue);
        }

        [MondFunction("next")]
        public int Next(int minValue, int maxValue)
        {
            return _random.Next(minValue, maxValue);
        }

        [MondFunction("nextDouble")]
        public double NextDouble()
        {
            return _random.NextDouble();
        }
    }
}
