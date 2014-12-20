using System;
using System.Collections.Generic;

namespace Mond.Binding
{
    public static partial class MondFunctionBinder
    {
        private static readonly Dictionary<Type, MondValueType[]> TypeCheckMap;
        private static readonly HashSet<Type> BasicTypes;
        private static readonly HashSet<Type> NumberTypes;

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

            // types with a direct conversion to/from MondValue
            BasicTypes = new HashSet<Type>
            {
                typeof(double),
                typeof(string),
                typeof(bool)
            };

            // types that can be casted to/from double
            NumberTypes = new HashSet<Type>
            {
                typeof(double),
                typeof(float),
                typeof(int),
                typeof(uint),
                typeof(short),
                typeof(ushort),
                typeof(sbyte),
                typeof(byte),
            };
        }
    }
}
