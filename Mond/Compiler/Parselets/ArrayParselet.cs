using System.Collections.Generic;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets
{
    class ArrayParselet : IPrefixParselet
    {
        public Expression Parse(Parser parser, Token token)
        {
            var values = new List<Expression>();

            if (!parser.MatchAndTake(TokenType.RightSquare))
            {
                var firstExpression = parser.ParseExpession();

                if (parser.MatchAndTake(TokenType.Colon))
                {
                    // list comprehension
                    return ParseListComprehension(parser, token, firstExpression);
                }

                // array initializer
                values.Add(firstExpression);

                while (!parser.Match(TokenType.RightSquare))
                {
                    if (!parser.Match(TokenType.Comma))
                        break;

                    parser.Take(TokenType.Comma);

                    // allow trailing comma
                    if (parser.Match(TokenType.RightSquare))
                        break;

                    var value = parser.ParseExpession();
                    values.Add(value);
                }

                parser.Take(TokenType.RightSquare);
                return new ArrayExpression(token, values);
            }

            // empty array
            return new ArrayExpression(token, values);
        }

        private static Expression ParseListComprehension(Parser parser, Token token, Expression firstExpression)
        {
            ComprehensionPart root = null;
            ComprehensionPart prev = null;

            while (true)
            {
                ComprehensionPart curr;

                if (parser.Match(TokenType.Identifier) && parser.Match(TokenType.In, 1))
                {
                    var identifier = parser.Take(TokenType.Identifier).Contents;

                    parser.Take(TokenType.In);

                    var list = parser.ParseExpession();

                    curr = new ListPart(token, identifier, list);
                }
                else
                {
                    var filter = parser.ParseExpession();
                    curr = new FilterPart(token, filter);
                }

                if (root == null)
                    root = curr;

                if (prev != null)
                    prev.Next = curr;

                prev = curr;

                if (!parser.Match(TokenType.Comma))
                    break;

                parser.Take(TokenType.Comma);
            }

            parser.Take(TokenType.RightSquare);

            prev.Next = new YieldPart(token, firstExpression);

            var body = new BlockExpression(new List<Expression>
            {
                root.Compile()
            });

            return new ListComprehensionExpression(token, body);
        }

        abstract class ComprehensionPart
        {
            public ComprehensionPart Next { protected get; set; }

            public abstract Expression Compile();
        }

        class YieldPart : ComprehensionPart
        {
            private readonly Token _token;
            private readonly Expression _value;

            public YieldPart(Token token, Expression value)
            {
                _token = token;
                _value = value;
            }

            public override Expression Compile()
            {
                return new YieldExpression(_token, _value);
            }
        }

        class ListPart : ComprehensionPart
        {
            private readonly Token _token;
            private readonly string _identifier;
            private readonly Expression _list;

            public ListPart(Token token, string identifier, Expression list)
            {
                _token = token;
                _identifier = identifier;
                _list = list;
            }

            public override Expression Compile()
            {
                var block = new BlockExpression(new List<Expression>
                {
                    Next.Compile()
                });

                return new ForeachExpression(_token, _identifier, _list, block);
            }
        }

        class FilterPart : ComprehensionPart
        {
            private readonly Token _token;
            private readonly Expression _filter;

            public FilterPart(Token token, Expression filter)
            {
                _token = token;
                _filter = filter;
            }

            public override Expression Compile()
            {
                var block = new BlockExpression(new List<Expression>
                {
                    Next.Compile()
                });

                var branches = new List<IfExpression.Branch>
                {
                    new IfExpression.Branch(_filter, block)
                };

                return new IfExpression(_token, branches, null);
            }
        }
    }
}
