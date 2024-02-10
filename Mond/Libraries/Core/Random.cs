using System;
using Mond.Binding;

namespace Mond.Libraries.Core
{
    [MondClass("Random")]
    internal partial class RandomClass
    {
        private readonly Random _random;

        [MondConstructor]
        public RandomClass() => _random = new Random();

        [MondConstructor]
        public RandomClass(int seed) => _random = new Random(seed);

        [MondFunction]
        public int Next() => _random.Next();

        [MondFunction]
        public int Next(int maxValue) => _random.Next(maxValue);

        [MondFunction]
        public int Next(int minValue, int maxValue) => _random.Next(minValue, maxValue);

        [MondFunction]
        public double NextDouble() => _random.NextDouble();
    }
}
