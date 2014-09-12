using Mond.Compiler.Expressions;
using System.Globalization;

namespace Mond.Compiler.Parselets
{
    class NumberParselet : IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token)
        {
            double value;

            if( token.Type == TokenType.HexNumber )
                value = (double)long.Parse( token.Contents, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture );
            else if( token.Type == TokenType.DecimalNumber )
                value = double.Parse( token.Contents, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture );
            else
                throw new MondCompilerException( token.FileName, token.Line, CompilerError.InvalidNumber, token.Contents );

            return new NumberExpression(token, value);
        }
    }
}
