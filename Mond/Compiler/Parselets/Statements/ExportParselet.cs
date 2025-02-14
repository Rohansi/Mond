using System.Linq;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    internal class ExportParselet : IStatementParselet
    {
        public Expression Parse(Parser parser, Token token, out bool trailingSemicolon)
        {
            Expression expr;
            if (parser.Match(TokenType.Const))
            {
                var constToken = parser.Take();
                expr = new VarParselet(true).Parse(parser, constToken, out trailingSemicolon);
            }
            else if (parser.Match(TokenType.Fun) || parser.Match(TokenType.Seq))
            {
                var typeToken = parser.Take();
                expr = typeToken.Type == TokenType.Fun
                    ? new FunctionParselet().Parse(parser, typeToken, out trailingSemicolon)
                    : new SequenceParselet().Parse(parser, typeToken, out trailingSemicolon);
            }
            else if (parser.MatchAndTake(TokenType.Multiply))
            {
                parser.Take(TokenType.From);
                var moduleName = ImportParselet.ParseModuleName(parser, out _);
                trailingSemicolon = true;
                return new ExportAllExpression(token, moduleName);
            }
            else
            {
                throw new MondCompilerException(token, CompilerError.ExportMustBeFollowedByKeywords);
            }

            if (expr is not IDeclarationExpression declaration)
            {
                throw new MondCompilerException(token, CompilerError.ExportMustBeFollowedByDeclaration);
            }

            expr.EndToken = parser.Previous;

            var identifiers = declaration.DeclaredIdentifiers;
            if (identifiers == null || !identifiers.Any())
            {
                throw new MondCompilerException(token, CompilerError.ExportMustBeFollowedByNonEmptyDeclaration);
            }

            return new ExportExpression(token, expr);
        }
    }
}
