using System;
using Mond.Binding;

namespace Mond.Libraries.Core
{
    [MondModule("Math")]
    internal class MathModule
    {
        [MondFunction]
        public static double Abs(double value) => Math.Abs(value);

        [MondFunction]
        public static double Acos(double d) => Math.Acos(d);

        [MondFunction]
        public static double Asin(double d) => Math.Asin(d);

        [MondFunction]
        public static double Atan(double d) => Math.Atan(d);

        [MondFunction]
        public static double Atan2(double y, double x) => Math.Atan2(y, x);

        [MondFunction]
        public static double Ceiling(double d) => Math.Ceiling(d);

        [MondFunction]
        public static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        [MondFunction]
        public static double Cos(double d) => Math.Cos(d);

        [MondFunction]
        public static double Cosh(double d) => Math.Cosh(d);

        [MondFunction]
        public static double Exp(double d) => Math.Exp(d);

        [MondFunction]
        public static double Floor(double d) => Math.Floor(d);

        [MondFunction]
        public static double Log(double d) => Math.Log(d);

        [MondFunction]
        public static double Log(double d, double b) => Math.Log(d, b);

        [MondFunction]
        public static double Log10(double d) => Math.Log10(d);

        [MondFunction]
        public static double Max(double x, double y) => Math.Max(x, y);

        [MondFunction]
        public static double Min(double x, double y) => Math.Min(x, y);

        [MondFunction]
        public static double Pow(double x, double y) => Math.Pow(x, y);

        [MondFunction]
        public static double Round(double d) => Math.Round(d);

        [MondFunction]
        public static double Sign(double d) => Math.Sign(d);

        [MondFunction]
        public static double Sin(double d) => Math.Sin(d);

        [MondFunction]
        public static double Sinh(double d) => Math.Sinh(d);

        [MondFunction]
        public static double Sqrt(double d) => Math.Sqrt(d);

        [MondFunction]
        public static double Tan(double d) => Math.Tan(d);

        [MondFunction]
        public static double Tanh(double d) => Math.Tanh(d);

        [MondFunction]
        public static double Truncate(double d) => Math.Truncate(d);
    }
}
