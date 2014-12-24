using System;
using System.Collections.Generic;

namespace Mond.Binding
{
    public static partial class MondFunctionBinder
    {
        internal static readonly Dictionary<Type, MondValueType[]> TypeCheckMap;
        internal static readonly HashSet<Type> BasicTypes;
        internal static readonly HashSet<Type> NumberTypes;

        static MondFunctionBinder()
        {
            var numberTypeArray = new [] { MondValueType.Number, MondValueType.Object };

            TypeCheckMap = new Dictionary<Type, MondValueType[]>
            {
                { typeof(double),       numberTypeArray },
                { typeof(float),        numberTypeArray },
                { typeof(int),          numberTypeArray },
                { typeof(uint),         numberTypeArray },
                { typeof(short),        numberTypeArray },
                { typeof(ushort),       numberTypeArray },
                { typeof(sbyte),        numberTypeArray },
                { typeof(byte),         numberTypeArray },

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
