using System;
using Mond.Binding;

namespace Mond.Repl.Modules
{
    [MondModule("Math")]
    public class MondMath
    {
        private static MondValue _binding;

        public static MondValue Binding
        {
            get
            {
                if (_binding != null)
                    return _binding;

                _binding = MondModuleBinder.Bind<MondMath>();

                _binding["PI"] = Math.PI;
                _binding["E"] = Math.E;

                return _binding;
            }
        }

        [MondFunction("abs")]
        public static double Abs(double value)
        {
            return Math.Abs(value);
        }

        [MondFunction("acos")]
        public static double Acos(double d)
        {
            return Math.Acos(d);
        }

        [MondFunction("asin")]
        public static double Asin(double d)
        {
            return Math.Asin(d);
        }

        [MondFunction("atan")]
        public static double Atan(double d)
        {
            return Math.Atan(d);
        }

        [MondFunction("atan2")]
        public static double Atan2(double y, double x)
        {
            return Math.Atan2(y, x);
        }

        [MondFunction("ceiling")]
        public static double Ceiling(double d)
        {
            return Math.Ceiling(d);
        }

        [MondFunction("cos")]
        public static double Cos(double d)
        {
            return Math.Cos(d);
        }

        [MondFunction("cosh")]
        public static double Cosh(double d)
        {
            return Math.Cosh(d);
        }

        [MondFunction("exp")]
        public static double Exp(double d)
        {
            return Math.Exp(d);
        }

        [MondFunction("floor")]
        public static double Floor(double d)
        {
            return Math.Floor(d);
        }

        [MondFunction("log")]
        public static double Log(double d)
        {
            return Math.Log(d);
        }

        [MondFunction("max")]
        public static double Max(double x, double y)
        {
            return Math.Max(x, y);
        }

        [MondFunction("min")]
        public static double Min(double x, double y)
        {
            return Math.Min(x, y);
        }

        [MondFunction("round")]
        public static double Round(double d)
        {
            return Math.Round(d);
        }

        [MondFunction("sign")]
        public static double Sign(double d)
        {
            return Math.Sign(d);
        }

        [MondFunction("sin")]
        public static double Sin(double d)
        {
            return Math.Sin(d);
        }

        [MondFunction("sinh")]
        public static double Sinh(double d)
        {
            return Math.Sinh(d);
        }

        [MondFunction("sqrt")]
        public static double Sqrt(double d)
        {
            return Math.Sqrt(d);
        }

        [MondFunction("tan")]
        public static double Tan(double d)
        {
            return Math.Tan(d);
        }

        [MondFunction("tanh")]
        public static double Tanh(double d)
        {
            return Math.Tanh(d);
        }

        [MondFunction("truncate")]
        public static double Truncate(double d)
        {
            return Math.Truncate(d);
        }
    }
}
