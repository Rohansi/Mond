using System.IO;
using System.Text;

namespace Mond
{
    class IndentTextWriter : TextWriter
    {
        private readonly TextWriter _writer;
        private readonly string _indentStr;

        public int Indent { get; set; }

        public IndentTextWriter(TextWriter writer, string indentStr = "  ")
        {
            _writer = writer;
            _indentStr = indentStr;
        }

        public override Encoding Encoding
        {
            get { return Encoding.Unicode; }
        }

        public override void Write(char value)
        {
            _writer.Write(value);
        }

        public void WriteIndent()
        {
            for (var i = 0; i < Indent; i++)
                Write(_indentStr);
        }
    }
}
