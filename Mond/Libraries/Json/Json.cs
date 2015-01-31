using System.Text;
using Mond.Binding;

namespace Mond.Libraries.Json
{
    [MondModule("Json")]
    internal partial class JsonModule
    {
        [MondFunction("serialize")]
        public static string Serialize(MondValue value)
        {
            var sb = new StringBuilder();

            SerializeImpl(value, sb, 0);

            return sb.ToString();
        }

        private static void SerializeImpl(MondValue value, StringBuilder sb, int depth)
        {
            if (depth >= 32)
                throw new MondRuntimeException("Json.serialize: maximum depth exceeded");

            var first = true;

            switch (value.Type)
            {
                case MondValueType.True:
                    sb.Append("true");
                    break;

                case MondValueType.False:
                    sb.Append("false");
                    break;

                case MondValueType.Null:
                case MondValueType.Undefined:
                    sb.Append("null");
                    break;

                case MondValueType.Number:
                    sb.Append((double)value);
                    break;

                case MondValueType.String:
                    SerializeString(value, sb);
                    break;

                case MondValueType.Object:
                    sb.Append('{');

                    foreach (var kvp in value.ObjectValue.Values)
                    {
                        if (kvp.Value == MondValue.Undefined)
                            continue;

                        if (first)
                            first = false;
                        else
                            sb.Append(',');

                        SerializeImpl(kvp.Key, sb, depth + 1);

                        sb.Append(':');

                        SerializeImpl(kvp.Value, sb, depth + 1);
                    }

                    sb.Append('}');
                    break;

                case MondValueType.Array:
                    sb.Append('[');

                    foreach (var v in value.ArrayValue)
                    {
                        if (first)
                            first = false;
                        else
                            sb.Append(',');

                        SerializeImpl(v, sb, depth + 1);
                    }

                    sb.Append(']');
                    break;

                default:
                    throw new MondRuntimeException("Json.serialize: can't serialize {0}s", value.Type.GetName());
            }
        }

        private static void SerializeString(string value, StringBuilder sb)
        {
            sb.Append('"');

            foreach (var c in value)
            {
                switch (c)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;

                    case '\"':
                        sb.Append("\\\"");
                        break;

                    case '\b':
                        sb.Append("\\b");
                        break;

                    case '\f':
                        sb.Append("\\f");
                        break;

                    case '\n':
                        sb.Append("\\n");
                        break;

                    case '\r':
                        sb.Append("\\r");
                        break;

                    case '\t':
                        sb.Append("\\t");
                        break;

                    default:
                        sb.Append(c);
                        break;
                }
            }

            sb.Append('"');
        }
    }
}
