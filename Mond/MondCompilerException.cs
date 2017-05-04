using JetBrains.Annotations;
using Mond.Compiler;
using Mond.Compiler.Expressions;

namespace Mond
{
    public class MondCompilerException : MondException
    {
        [StringFormatMethod("format")]
        internal MondCompilerException(string fileName, int line, int column, string format, params object[] args)
            : base($"{fileName ?? "null"}(line {line}:{column}): {string.Format(format, args)}")
        {
            
        }

        [StringFormatMethod("format")]
        internal MondCompilerException(Token token, string format, params object[] args)
            : this(token.FileName, token.Line, token.Column, format, args)
        {
            
        }

        [StringFormatMethod("format")]
        internal MondCompilerException(Expression expression, string format, params object[] args)
            : this(expression.Token.FileName, expression.Token.Line, expression.Token.Column, format, args)
        {

        }
    }
}
