using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    class VarParselet : IStatementParselet
    {
        public class DestructuringField
        {
            public Token FieldName { get; private set; }
            public Token AliasName { get; private set; }

            internal DestructuringField(Token field, Token alias)
            {
                FieldName = field;
                AliasName = alias;
            }
        }

        public class DestructuringIndex
        {
            public Token Name { get; private set; }
            public bool IsSlice { get; private set; }
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

                return DestructureObject(token, fields, parser.ParseExpression(), _isReadOnly);
            }

            if (parser.MatchAndTake(TokenType.LeftSquare))
            {
                var indecies = ParseArrayDestructuring(parser);
                parser.Take(TokenType.Assign);

                return DestructureArray(token, indecies, parser.ParseExpression(), _isReadOnly);
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

        internal static ReadOnlyCollection<DestructuringField> ParseObjectDestructuring(Parser parser)
        {
            var fields = new List<DestructuringField>();
            do
            {
                var field = parser.Take(TokenType.Identifier);
                var name = parser.MatchAndTake(TokenType.Colon) ? parser.Take(TokenType.Identifier) : field;
                fields.Add(new DestructuringField(field, name));
            } while (parser.MatchAndTake(TokenType.Comma));

            parser.Take(TokenType.RightBrace);

            return fields.AsReadOnly();
        }

        internal static ReadOnlyCollection<DestructuringIndex> ParseArrayDestructuring(Parser parser)
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

            return indecies.AsReadOnly();
        }

        internal static VarExpression DestructureObject(Token baseToken, IList<DestructuringField> fields, Expression obj, bool isReadOnly)
        {
            var declatations = fields.Select(delegate (DestructuringField field)
             {
                 var initializer = new FieldExpression(field.FieldName, obj);
                 return new VarExpression.Declaration(field.AliasName.Contents, initializer);
             });

            return new VarExpression(baseToken, declatations.ToList(), isReadOnly);
        }

        internal static VarExpression DestructureArray(Token baseToken, IList<DestructuringIndex> indecies, Expression array, bool isReadOnly)
        {
            var arrayToken = array != null ? array.Token : baseToken;
            var startIndex = default(NumberExpression);
            var declarations = indecies.Select(delegate (DestructuringIndex index, int i)
            {
                index.StartIndex = startIndex ?? new NumberExpression(arrayToken, i);
                var indexer = default(Expression);

                if (index.IsSlice)
                {
                    var remaining = indecies.Skip(i + 1).Count();
                    index.EndIndex = new NumberExpression(index.StartIndex.Token, -remaining - 1);
                    startIndex = new NumberExpression(index.StartIndex.Token, -remaining);

                    indexer = new SliceExpression(arrayToken, array, index.StartIndex, index.EndIndex, null);
                }
                else
                {
                    indexer = new IndexerExpression(arrayToken, array, index.StartIndex);
                }

                return new VarExpression.Declaration(index.Name.Contents, indexer);
            });

            return new VarExpression(baseToken, declarations.ToList());
        }
    }
}
