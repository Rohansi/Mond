using System.Collections.Generic;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    class VarParselet : IStatementParselet
    {
        public class DestructuringField
        {
            public Token FieldName { get; }
            public Token AliasName { get; }

            internal DestructuringField(Token field, Token alias)
            {
                FieldName = field;
                AliasName = alias;
            }
        }

        public class DestructuringIndex
        {
            public Token Name { get; }
            public bool IsSlice { get; }
            public Expression StartIndex { get; internal set; }
            public Expression EndIndex { get; internal set; }

            internal DestructuringIndex(Token name, bool slice)
            {
                Name = name;
                IsSlice = slice;
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
            {
                var fields = ParseObjectDestructuring(parser);
                parser.Take(TokenType.Assign);

                return new DestructuredObjectExpression(token, fields, parser.ParseExpression(), _isReadOnly);
            }

            if (parser.MatchAndTake(TokenType.LeftSquare))
            {
                var indices = ParseArrayDestructuring(parser);
                parser.Take(TokenType.Assign);

                return new DestructuredArrayExpression(token, indices, parser.ParseExpression(), _isReadOnly);
            }

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

        internal static List<DestructuredObjectExpression.Field> ParseObjectDestructuring(Parser parser)
        {
            var fields = new List<DestructuredObjectExpression.Field>();
            do
            {
                var field = parser.Take(TokenType.Identifier);
                var alias = parser.MatchAndTake(TokenType.Colon) ? parser.Take(TokenType.Identifier).Contents : null;
                fields.Add(new DestructuredObjectExpression.Field(field.Contents, alias));
            } while (parser.MatchAndTake(TokenType.Comma));

            parser.Take(TokenType.RightBrace);

            return fields;
        }

        internal static List<DestructuredArrayExpression.Index> ParseArrayDestructuring(Parser parser)
        {
            var indices = new List<DestructuredArrayExpression.Index>();
            var hasEllipsis = false;
            do
            {
                var slice = parser.MatchAndTake(TokenType.Ellipsis);
                var name = parser.Take(TokenType.Identifier);

                if (hasEllipsis && slice)
                    throw new MondCompilerException(name, CompilerError.MultipleDestructuringSlices);

                if (slice)
                    hasEllipsis = true;

                indices.Add(new DestructuredArrayExpression.Index(name.Contents, slice));
            } while (parser.MatchAndTake(TokenType.Comma));

            parser.Take(TokenType.RightSquare);

            return indices;
        }
    }
}
