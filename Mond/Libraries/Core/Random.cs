using System;
using Mond.Binding;

namespace Mond.Libraries.Core
{
    /// <summary>
    /// Generates pseudo random numbers, optionally from a fixed seed.
    /// </summary>
    [MondClass("Random")]
    internal partial class RandomClass
    {
        private readonly Random _random;

        /// <summary>
        /// Creates a generator, seeded from the given value or from the clock when none is given.
        /// </summary>
        [MondConstructor]
        public RandomClass() => _random = new Random();

        /// <summary>
        /// Creates a generator, seeded from the given value or from the clock when none is given.
        /// </summary>
        [MondConstructor]
        public RandomClass(int seed) => _random = new Random(seed);

        /// <summary>
        /// Returns a whole number, below maxValue and at or above minValue when those are given.
        /// </summary>
        [MondFunction]
        public int Next() => _random.Next();

        /// <summary>
        /// Returns a whole number, below maxValue and at or above minValue when those are given.
        /// </summary>
        [MondFunction]
        public int Next(int maxValue) => _random.Next(maxValue);

        /// <summary>
        /// Returns a whole number, below maxValue and at or above minValue when those are given.
        /// </summary>
        [MondFunction]
        public int Next(int minValue, int maxValue) => _random.Next(minValue, maxValue);

        /// <summary>
        /// Returns a number from 0 up to but not including 1.
        /// </summary>
        [MondFunction]
        public double NextDouble() => _random.NextDouble();
    }
}
