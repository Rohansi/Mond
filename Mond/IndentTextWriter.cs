﻿using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Mond
{
    class IndentTextWriter : TextWriter
    {
        private readonly TextWriter _writer;
        private readonly string _indentStr;
        private bool _shouldIndent;

        public int Indent { get; set; }

        public IndentTextWriter(TextWriter writer, string indentStr = "  ")
        {
            _writer = writer;
            _indentStr = indentStr;
            _shouldIndent = false;
        }

        public override Encoding Encoding
        {
            get { return Encoding.Unicode; }
        }

        public override IFormatProvider FormatProvider
        {
            get { return CultureInfo.InvariantCulture; }
        }

        public override void Write(char value)
        {
            if (_shouldIndent)
            {
                _shouldIndent = false; // shouldIndent must be cleared first
                WriteIndent();
            }

            _writer.Write(value);
        }

        public override void WriteLine()
        {
            base.WriteLine();

            _shouldIndent = true; // defer indenting until the next Write
        }

        public override void WriteLine(string value)
        {
            Write(value);
            WriteLine();
        }

        private void WriteIndent()
        {
            for (var i = 0; i < Indent; i++)
                Write(_indentStr);
        }
    }
}