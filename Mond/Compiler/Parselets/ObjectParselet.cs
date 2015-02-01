using System.Collections.Generic;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;
using Mond.Compiler.Parselets.Statements;

namespace Mond.Compiler.Parselets
{
    class ObjectParselet : IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token)
        {
            var values = new List<KeyValuePair<string, Expression>>();

            while (!parser.Match(TokenType.RightBrace))
            {
                string key;
                Expression value = null;

                if (parser.Match(TokenType.Identifier))
                {
                    var identifier = parser.Take(TokenType.Identifier);
                    key = identifier.Contents;

                    if (parser.Match(TokenType.Comma) || parser.Match(TokenType.RightBrace))
                    {
                        value = new IdentifierExpression(identifier);
                    }
                }
                else if (parser.Match(TokenType.String))
                {
                    key = parser.Take(TokenType.String).Contents;
                }
                else if (parser.Match(TokenType.Fun))
                {
                    var funToken = parser.Take(TokenType.Fun);
                    var function = (FunctionExpression)new FunctionParselet().Parse(parser, funToken);

                    if (function.Name == null)
                        throw new MondCompilerException(funToken, CompilerError.ObjectFunctionNotNamed);

                    function.StoreInNameVariable = false;

                    key = function.Name;
                    value = function;
                }
                else if (parser.Match(TokenType.Seq))
                {
                    var seqToken = parser.Take(TokenType.Seq);
                    var sequence = (SequenceExpression)new SequenceParselet().Parse(parser, seqToken);

                    if (sequence.Name == null)
                        throw new MondCompilerException(seqToken, CompilerError.ObjectFunctionNotNamed);

                    sequence.StoreInNameVariable = false;

                    key = sequence.Name;
                    value = sequence;
                }
                else
                {
                    var errorToken = parser.Take();

                    throw new MondCompilerException(errorToken, CompilerError.ExpectedButFound, "Identifier, String, Fun or Seq", errorToken);
                }

                if (value == null)
                {
                    parser.Take(TokenType.Colon);
                    value = parser.ParseExpression();
                }

                values.Add(new KeyValuePair<string, Expression>(key, value));

                if (!parser.Match(TokenType.Comma))
                    break;

                parser.Take(TokenType.Comma);
            }

            parser.Take(TokenType.RightBrace);

            return new ObjectExpression(token, values);
        }
    }
}
