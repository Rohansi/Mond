using System.Globalization;
using System.Text;

namespace Mond.SourceGenerator;

/// <summary>
/// Minimal pretty-printing JSON writer. Analyzers target netstandard2.0 where
/// <c>System.Text.Json</c> is not available, and the output shape is small enough that pulling in a
/// dependency would not be worth it.
/// </summary>
internal sealed class JsonWriter
{
    private readonly StringBuilder _sb = new();
    private int _indent;
    private bool _needsComma;

    public void OpenObject() => Open('{');

    public void CloseObject() => Close('}');

    public void OpenArray() => Open('[');

    public void CloseArray() => Close(']');

    public void Name(string name)
    {
        WriteSeparator();
        WriteString(name);
        _sb.Append(": ");
        _needsComma = false;
    }

    public void Value(string value)
    {
        WriteSeparator();

        if (value == null)
        {
            _sb.Append("null");
        }
        else
        {
            WriteString(value);
        }

        _needsComma = true;
    }

    public void Value(int value)
    {
        WriteSeparator();
        _sb.Append(value.ToString(CultureInfo.InvariantCulture));
        _needsComma = true;
    }

    public void Property(string name, string value)
    {
        Name(name);
        Value(value);
    }

    public void Property(string name, int value)
    {
        Name(name);
        Value(value);
    }

    public override string ToString() => _sb.ToString() + "\n";

    private void Open(char bracket)
    {
        WriteSeparator();
        _sb.Append(bracket);
        _indent++;
        _needsComma = false;
    }

    private void Close(char bracket)
    {
        _indent--;

        // an empty object or array stays on one line
        if (_needsComma || _sb[_sb.Length - 1] != (bracket == '}' ? '{' : '['))
        {
            WriteNewLine();
        }

        _sb.Append(bracket);
        _needsComma = true;
    }

    private void WriteSeparator()
    {
        if (_needsComma)
        {
            _sb.Append(',');
        }

        // the very first token starts the document, and values after a name stay on the same line
        if (_sb.Length > 0 && (_needsComma || _sb[_sb.Length - 1] is '{' or '['))
        {
            WriteNewLine();
        }

        _needsComma = false;
    }

    private void WriteNewLine()
    {
        _sb.Append('\n');
        _sb.Append(' ', _indent * 2);
    }

    private void WriteString(string value)
    {
        _sb.Append('"');

        foreach (var c in value)
        {
            switch (c)
            {
                case '"':
                    _sb.Append("\\\"");
                    break;
                case '\\':
                    _sb.Append("\\\\");
                    break;
                case '\b':
                    _sb.Append("\\b");
                    break;
                case '\f':
                    _sb.Append("\\f");
                    break;
                case '\n':
                    _sb.Append("\\n");
                    break;
                case '\r':
                    _sb.Append("\\r");
                    break;
                case '\t':
                    _sb.Append("\\t");
                    break;
                default:
                    if (c < ' ' || c > '~')
                    {
                        _sb.Append("\\u");
                        _sb.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        _sb.Append(c);
                    }

                    break;
            }
        }

        _sb.Append('"');
    }
}
