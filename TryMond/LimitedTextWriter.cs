using System.Text;

namespace TryMond;

internal class LimitedTextWriter : TextWriter
{
    private readonly TextWriter _writer;
    private readonly int _maxChars;
    private readonly int _maxLines;
    private int _chars;
    private int _lines;

    public LimitedTextWriter(TextWriter writer, int maxChars, int maxLines)
    {
        _writer = writer;
        _maxChars = maxChars;
        _maxLines = maxLines;
        _chars = 0;
        _lines = 0;
    }

    public override Encoding Encoding
    {
        get { return _writer.Encoding; }
    }

    public override void Write(char value)
    {
        if (_chars >= _maxChars || _lines >= _maxLines)
            return;

        if (value == '\n')
            _lines++;

        _writer.Write(value);
        _chars++;
    }
}
