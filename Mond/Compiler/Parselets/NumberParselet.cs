using Mond.Compiler.Expressions;
using System.Globalization;

namespace Mond.Compiler.Parselets
{
    class NumberParselet : IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token)
        {
            double value;

            try
            {
                value = (double)long.Parse( token.Contents, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture );
            }
            catch
            {
                value = double.Parse( token.Contents, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture );
            }

            return new NumberExpression(token, value);
        }
    }
}
