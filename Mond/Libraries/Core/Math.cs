using System;
using Mond.Binding;

namespace Mond.Libraries.Core
{
    /// <summary>
    /// Common mathematical functions, along with the constants PI and E.
    /// </summary>
    [MondModule("Math")]
    internal static partial class MathModule
    {
        partial class Library
        {
            partial void ModifyObject(MondValue obj)
            {
                obj["PI"] = Math.PI;
                obj["E"] = Math.E;
            }
        }

        /// <summary>
        /// Returns the value without its sign.
        /// </summary>
        [MondFunction]
        public static double Abs(double value) => Math.Abs(value);

        /// <summary>
        /// Returns the angle in radians whose cosine is the given value.
        /// </summary>
        [MondFunction]
        public static double Acos(double d) => Math.Acos(d);

        /// <summary>
        /// Returns the angle in radians whose sine is the given value.
        /// </summary>
        [MondFunction]
        public static double Asin(double d) => Math.Asin(d);

        /// <summary>
        /// Returns the angle in radians whose tangent is the given value.
        /// </summary>
        [MondFunction]
        public static double Atan(double d) => Math.Atan(d);

        /// <summary>
        /// Returns the angle in radians to the point (x, y), using both signs to pick the quadrant.
        /// </summary>
        [MondFunction]
        public static double Atan2(double y, double x) => Math.Atan2(y, x);

        /// <summary>
        /// Rounds the value up to the nearest whole number.
        /// </summary>
        [MondFunction]
        public static double Ceiling(double d) => Math.Ceiling(d);

        /// <summary>
        /// Returns the value limited to the range between min and max.
        /// </summary>
        [MondFunction]
        public static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// Returns the cosine of an angle given in radians.
        /// </summary>
        [MondFunction]
        public static double Cos(double d) => Math.Cos(d);

        /// <summary>
        /// Returns the hyperbolic cosine of the value.
        /// </summary>
        [MondFunction]
        public static double Cosh(double d) => Math.Cosh(d);

        /// <summary>
        /// Returns E raised to the given power.
        /// </summary>
        [MondFunction]
        public static double Exp(double d) => Math.Exp(d);

        /// <summary>
        /// Rounds the value down to the nearest whole number.
        /// </summary>
        [MondFunction]
        public static double Floor(double d) => Math.Floor(d);

        /// <summary>
        /// Returns the logarithm of the value, in base E unless another base is given.
        /// </summary>
        [MondFunction]
        public static double Log(double d) => Math.Log(d);

        /// <summary>
        /// Returns the logarithm of the value, in base E unless another base is given.
        /// </summary>
        [MondFunction]
        public static double Log(double d, double b) => Math.Log(d, b);

        /// <summary>
        /// Returns the base 10 logarithm of the value.
        /// </summary>
        [MondFunction]
        public static double Log10(double d) => Math.Log10(d);

        /// <summary>
        /// Returns the larger of the two values.
        /// </summary>
        [MondFunction]
        public static double Max(double x, double y) => Math.Max(x, y);

        /// <summary>
        /// Returns the smaller of the two values.
        /// </summary>
        [MondFunction]
        public static double Min(double x, double y) => Math.Min(x, y);

        /// <summary>
        /// Returns x raised to the power of y.
        /// </summary>
        [MondFunction]
        public static double Pow(double x, double y) => Math.Pow(x, y);

        /// <summary>
        /// Rounds the value to the nearest whole number.
        /// </summary>
        [MondFunction]
        public static double Round(double d) => Math.Round(d);

        /// <summary>
        /// Returns -1, 0, or 1 depending on the sign of the value.
        /// </summary>
        [MondFunction]
        public static double Sign(double d) => Math.Sign(d);

        /// <summary>
        /// Returns the sine of an angle given in radians.
        /// </summary>
        [MondFunction]
        public static double Sin(double d) => Math.Sin(d);

        /// <summary>
        /// Returns the hyperbolic sine of the value.
        /// </summary>
        [MondFunction]
        public static double Sinh(double d) => Math.Sinh(d);

        /// <summary>
        /// Returns the square root of the value.
        /// </summary>
        [MondFunction]
        public static double Sqrt(double d) => Math.Sqrt(d);

        /// <summary>
        /// Returns the tangent of an angle given in radians.
        /// </summary>
        [MondFunction]
        public static double Tan(double d) => Math.Tan(d);

        /// <summary>
        /// Returns the hyperbolic tangent of the value.
        /// </summary>
        [MondFunction]
        public static double Tanh(double d) => Math.Tanh(d);

        /// <summary>
        /// Discards the fractional part of the value.
        /// </summary>
        [MondFunction]
        public static double Truncate(double d) => Math.Truncate(d);
    }
}
