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
                { typeof(double),       new [] { MondValueType.Number, MondValueType.Object } },
                { typeof(float),        new [] { MondValueType.Number, MondValueType.Object } },
                { typeof(int),          new [] { MondValueType.Number, MondValueType.Object } },
                { typeof(uint),         new [] { MondValueType.Number, MondValueType.Object } },
                { typeof(short),        new [] { MondValueType.Number, MondValueType.Object } },
                { typeof(ushort),       new [] { MondValueType.Number, MondValueType.Object } },
                { typeof(sbyte),        new [] { MondValueType.Number, MondValueType.Object } },
                { typeof(byte),         new [] { MondValueType.Number, MondValueType.Object } },

                { typeof(string),       new [] { MondValueType.String, MondValueType.Object } },

                { typeof(bool),         new [] { MondValueType.True, MondValueType.False, MondValueType.Object } }
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
