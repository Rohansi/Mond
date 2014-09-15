using System;

namespace Mond
{
    public enum MondValueType
    {
        Undefined, Null, True, False, Object, Array, Number, String, Function
    }

    public static class MondValueTypeExtensions
    {
        public static string GetName(this MondValueType type)
        {
            switch (type)
            {
                case MondValueType.Undefined:
                    return "undefined";

                case MondValueType.Null:
                    return "null";

                case MondValueType.True:
                case MondValueType.False:
                    return "bool";

                case MondValueType.Object:
                    return "object";

                case MondValueType.Array:
                    return "array";

                case MondValueType.Number:
                    return "number";

                case MondValueType.String:
                    return "string";

                case MondValueType.Function:
                    return "function";

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
