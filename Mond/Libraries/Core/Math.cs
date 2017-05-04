using System;
using Mond.Binding;

namespace Mond.Libraries.Core
{
    [MondModule("Math")]
    internal class MathModule
    {
        [MondFunction("abs")]
        public static double Abs(double value) => Math.Abs(value);

        [MondFunction("acos")]
        public static double Acos(double d) => Math.Acos(d);

        [MondFunction("asin")]
        public static double Asin(double d) => Math.Asin(d);

        [MondFunction("atan")]
        public static double Atan(double d) => Math.Atan(d);

        [MondFunction("atan2")]
        public static double Atan2(double y, double x) => Math.Atan2(y, x);

        [MondFunction("ceiling")]
        public static double Ceiling(double d) => Math.Ceiling(d);

        [MondFunction("cos")]
        public static double Cos(double d) => Math.Cos(d);

        [MondFunction("cosh")]
        public static double Cosh(double d) => Math.Cosh(d);

        [MondFunction("exp")]
        public static double Exp(double d) => Math.Exp(d);

        [MondFunction("floor")]
        public static double Floor(double d) => Math.Floor(d);

        [MondFunction("log")]
        public static double Log(double d) => Math.Log(d);

        [MondFunction("max")]
        public static double Max(double x, double y) => Math.Max(x, y);

        [MondFunction("min")]
        public static double Min(double x, double y) => Math.Min(x, y);

        [MondFunction("round")]
        public static double Round(double d) => Math.Round(d);

        [MondFunction("sign")]
        public static double Sign(double d) => Math.Sign(d);

        [MondFunction("sin")]
        public static double Sin(double d) => Math.Sin(d);

        [MondFunction("sinh")]
        public static double Sinh(double d) => Math.Sinh(d);

        [MondFunction("sqrt")]
        public static double Sqrt(double d) => Math.Sqrt(d);

        [MondFunction("tan")]
        public static double Tan(double d) => Math.Tan(d);

        [MondFunction("tanh")]
        public static double Tanh(double d) => Math.Tanh(d);

        [MondFunction("truncate")]
        public static double Truncate(double d) => Math.Truncate(d);
    }
}
