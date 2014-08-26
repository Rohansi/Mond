using System;
using System.IO;
using System.Text;

namespace Mond
{
    public partial class MondValue
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
                SerializeImpl(writer);
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
                SerializeImpl(writer);
            }
        }

        private void SerializeImpl(IndentTextWriter writer)
        {
            switch (Type)
            {
                case MondValueType.Undefined:
                    writer.Write("undefined");
                    break;

                case MondValueType.Null:
                    writer.Write("null");
                    break;

                case MondValueType.True:
                    writer.Write("true");
                    break;

                case MondValueType.False:
                    writer.Write("false");
                    break;

                case MondValueType.Object:
                    writer.WriteLine("{");
                    writer.Indent++;

                    foreach (var objValue in ObjectValue.Values)
                    {
                        writer.WriteIndent();
                        objValue.Key.SerializeImpl(writer);
                        writer.Write(": ");
                        objValue.Value.SerializeImpl(writer);
                        writer.Write(",");
                        writer.WriteLine();
                    }

                    writer.Indent--;
                    writer.WriteIndent();
                    writer.Write("}");
                    break;

                case MondValueType.Array:
                    writer.WriteLine("[");
                    writer.Indent++;

                    foreach (var arrValue in ArrayValue)
                    {
                        writer.WriteIndent();
                        arrValue.SerializeImpl(writer);
                        writer.WriteLine(", ");
                    }

                    writer.Indent--;
                    writer.WriteIndent();
                    writer.Write("]");
                    break;

                case MondValueType.Number:
                    writer.Write("{0:R}", _numberValue);
                    break;

                case MondValueType.String:
                    writer.Write("\"{0}\"", _stringValue);
                    break;

                case MondValueType.Function:
                    writer.Write("function");
                    break;

                case MondValueType.UserData:
                    writer.Write("userdata");
                    break;

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
