using System.IO;
using System.Text;

namespace Mond
{
    public partial struct MondValue
    {
        /// <summary>
        /// Serialize the value to a string.
        /// </summary>
        public string Serialize()
        {
            var stringBuilder = new StringBuilder();

            using (var stringWriter = new StringWriter(stringBuilder))
            using (var writer = new IndentTextWriter(stringWriter))
            {
                SerializeImpl(writer, 0);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Serialize the value to a TextWriter.
        /// </summary>
        public void Serialize(TextWriter textWriter)
        {
            using (var writer = new IndentTextWriter(textWriter))
            {
                SerializeImpl(writer, 0);
            }
        }

        private bool SerializeImpl(IndentTextWriter writer, int depth)
        {
            if (depth >= 32)
            {
                writer.Write("< max depth reached >");
                return false;
            }

            bool first = true;

            switch (Type)
            {
                case MondValueType.True:
                    writer.Write("true");
                    break;

                case MondValueType.False:
                    writer.Write("false");
                    break;

                case MondValueType.Object:
                    if (TryDispatch("__serialize", out var result, this))
                    {
                        if (!result.SerializeImpl(writer, depth + 1))
                            return false;

                        break;
                    }

                    writer.WriteLine("{");
                    writer.Indent++;

                    foreach (var objValue in AsDictionary)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            writer.Write(",");
                            writer.WriteLine();
                        }

                        if (!objValue.Key.SerializeImpl(writer, depth + 1))
                            break;

                        writer.Write(": ");

                        if (!objValue.Value.SerializeImpl(writer, depth + 1))
                            break;
                    }

                    writer.WriteLine();
                    writer.Indent--;
                    writer.Write("}");
                    break;

                case MondValueType.Array:
                    writer.WriteLine("[");
                    writer.Indent++;

                    foreach (var arrValue in ArrayValue)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            writer.Write(",");
                            writer.WriteLine();
                        }

                        if (!arrValue.SerializeImpl(writer, depth + 1))
                            break;
                    }

                    writer.WriteLine();
                    writer.Indent--;
                    writer.Write("]");
                    break;

                case MondValueType.Number:
                    writer.Write("{0:R}", _numberValue);
                    break;

                case MondValueType.String:
                    SerializeString(writer, _stringValue);
                    break;

                default:
                    writer.Write(Type.GetName());
                    break;
            }

            return true;
        }

        private static void SerializeString(TextWriter writer, string value)
        {
            writer.Write('"');

            foreach (var c in value)
            {
                switch (c)
                {
                    case '\\':
                        writer.Write("\\\\");
                        break;

                    case '\"':
                        writer.Write("\\\"");
                        break;

                    case '\b':
                        writer.Write("\\b");
                        break;

                    case '\f':
                        writer.Write("\\f");
                        break;

                    case '\n':
                        writer.Write("\\n");
                        break;

                    case '\r':
                        writer.Write("\\r");
                        break;

                    case '\t':
                        writer.Write("\\t");
                        break;

                    default:
                        writer.Write(c);
                        break;
                }
            }

            writer.Write('"');
        }
    }
}
