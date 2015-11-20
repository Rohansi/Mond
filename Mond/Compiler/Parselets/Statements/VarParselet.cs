using System.Linq;
using System.Collections.Generic;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    class VarParselet : IStatementParselet
    {
        private class DestructuringField
        {
            public Token FieldName { get; private set; }
            public Token AliasName { get; private set; }

            public DestructuringField(Token field, Token alias)
            {
                FieldName = field;
                AliasName = alias;
            }
        }

        private class DestructuringIndex
        {
            public Token Name { get; private set; }
            public bool Slice { get; private set; }
            public Expression StartIndex { get; set; }
            public Expression EndIndex { get; set; }

            public DestructuringIndex(Token name, bool slice)
            {
                Name = name;
                Slice = slice;
            }
        }

        private readonly bool _isReadOnly;

        public VarParselet(bool isReadOnly)
        {
            _isReadOnly = isReadOnly;
        }

        public Expression Parse(Parser parser, Token token, out bool trailingSemicolon)
        {
            trailingSemicolon = true;

            if (parser.MatchAndTake(TokenType.LeftBrace))
                return ParseObjectDestructuring(parser, token);

            if (parser.MatchAndTake(TokenType.LeftSquare))
                return ParseArrayDestructuring(parser, token);

            var declarations = new List<VarExpression.Declaration>();
            do
            {
                var identifier = parser.Take(TokenType.Identifier);
                Expression initializer = null;

                if (_isReadOnly || parser.Match(TokenType.Assign))
                {
                    parser.Take(TokenType.Assign);
                    initializer = parser.ParseExpression();
                }

                var declaration = new VarExpression.Declaration(identifier.Contents, initializer);
                declarations.Add(declaration);
            } while (parser.MatchAndTake(TokenType.Comma));

            return new VarExpression(token, declarations, _isReadOnly);
        }

        private Expression ParseObjectDestructuring(Parser parser, Token token)
        {
            var fields = new List<DestructuringField>();
            do
            {
                var field = parser.Take(TokenType.Identifier);
                var name = parser.MatchAndTake(TokenType.Colon) ? parser.Take(TokenType.Identifier) : field;
                fields.Add(new DestructuringField(field, name));
            } while (parser.MatchAndTake(TokenType.Comma));

            parser.Take(TokenType.RightBrace);
            parser.Take(TokenType.Assign);
            var obj = parser.ParseExpression();

            var declatations = fields.Select(delegate (DestructuringField field)
            {
                var initializer = new FieldExpression(field.FieldName, obj);
                return new VarExpression.Declaration(field.AliasName.Contents, initializer);
            });

            return new BlockExpression(token, new[]
            {
                new VarExpression(token, declatations.ToList(), _isReadOnly)
            });
        }

        private Expression ParseArrayDestructuring(Parser parser, Token token)
        {
            var indecies = new List<DestructuringIndex>();
            var hasEllipsis = false;
            do
            {
                var slice = parser.MatchAndTake(TokenType.Ellipsis);
                var name = parser.Take(TokenType.Identifier);

                if (hasEllipsis && slice)
                    throw new MondCompilerException(name, CompilerError.MultipleDestructuringSlices);

                if (slice && !hasEllipsis)
                    hasEllipsis = true;

                indecies.Add(new DestructuringIndex(name, slice));
            } while (parser.MatchAndTake(TokenType.Comma));

            parser.Take(TokenType.RightSquare);
            parser.Take(TokenType.Assign);
            var array = parser.ParseExpression();

            var lengthToken = new Token(token, TokenType.Identifier, "length");
            var lengthField = new FieldExpression(lengthToken, array);
            var lengthCall = new CallExpression(lengthToken, lengthField, new List<Expression>());
            var startIndex = default(Expression);
            var declarations = indecies.Select(delegate (DestructuringIndex index, int i)
            {
                index.StartIndex = startIndex ?? new NumberExpression(array.Token, i);
                if (index.Slice)
                {
                    var subtractToken = new Token(index.StartIndex.Token, TokenType.Subtract, "-");
                    var subtract = new BinaryOperatorExpression(subtractToken, lengthCall, new NumberExpression(subtractToken, 1));
                    index.EndIndex = new BinaryOperatorExpression(subtractToken, subtract, index.StartIndex);
                    startIndex = new BinaryOperatorExpression(subtractToken, lengthCall, index.StartIndex);
                }

                var indexer = default(Expression);
                if (index.Slice)
                    indexer = new SliceExpression(array.Token, array, index.StartIndex, index.EndIndex, null);
                else
                    indexer = new IndexerExpression(array.Token, array, index.StartIndex);

                return new VarExpression.Declaration(index.Name.Contents, indexer);
            });

            return new BlockExpression(token, new[]
            {
                new VarExpression(token, declarations.ToList())
            });
        }
    }
}
