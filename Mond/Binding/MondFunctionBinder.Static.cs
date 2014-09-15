using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Mond.Binding
{
    public static partial class MondFunctionBinder
    {
        private static readonly Dictionary<Type, MondValueType[]> TypeCheckMap;
        private static readonly Dictionary<Type, Func<Expression, Expression>> ConversionMap;

        static MondFunctionBinder()
        {
            TypeCheckMap = new Dictionary<Type, MondValueType[]>
            {
                { typeof(double),       new [] { MondValueType.Number } },
                { typeof(float),        new [] { MondValueType.Number } },
                { typeof(int),          new [] { MondValueType.Number } },
                { typeof(uint),         new [] { MondValueType.Number } },
                { typeof(short),        new [] { MondValueType.Number } },
                { typeof(ushort),       new [] { MondValueType.Number } },
                { typeof(sbyte),        new [] { MondValueType.Number } },
                { typeof(byte),         new [] { MondValueType.Number } },

                { typeof(string),       new [] { MondValueType.String } },

                { typeof(bool),         new [] { MondValueType.True, MondValueType.False } }
            };

            ConversionMap = new Dictionary<Type, Func<Expression, Expression>>
            {
                { typeof(float),        e => Expression.Convert(e, typeof(double)) },
                { typeof(int),          e => Expression.Convert(e, typeof(double)) },
                { typeof(uint),         e => Expression.Convert(e, typeof(double)) },
                { typeof(short),        e => Expression.Convert(e, typeof(double)) },
                { typeof(ushort),       e => Expression.Convert(e, typeof(double)) },
                { typeof(sbyte),        e => Expression.Convert(e, typeof(double)) },
                { typeof(byte),         e => Expression.Convert(e, typeof(double)) }
            };
        }
    }
}
