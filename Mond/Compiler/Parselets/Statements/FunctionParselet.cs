using System.Collections.Generic;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    class FunctionParselet : IStatementParselet
    {
        public bool TrailingSemicolon { get { return false; } }

        public Expression Parse(Parser parser, Token token)
        {
            var name = parser.Take(TokenType.Identifier).Contents;

            parser.Take(TokenType.LeftParen);

            var arguments = new List<string>();

            if (!parser.Match(TokenType.RightParen))
            {
                while (true)
                {
                    var identifier = parser.Take(TokenType.Identifier);
                    arguments.Add(identifier.Contents);

                    if (parser.Match(TokenType.RightParen))
                        break;

                    parser.Take(TokenType.Comma);
                }
            }
            
            parser.Take(TokenType.RightParen);

            var block = parser.ParseBlock(false);
            return new FunctionExpression(token, name, arguments, block);
        }
    }
}
