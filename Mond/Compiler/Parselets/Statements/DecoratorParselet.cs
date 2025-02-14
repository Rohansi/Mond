using System.Collections.Generic;
using System.Linq;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    internal class DecoratorParselet : IStatementParselet
    {
        public Expression Parse(Parser parser, Token token, out bool trailingSemicolon)
        {
            trailingSemicolon = false;
            var exprs = new List<Expression>
            {
                parser.ParseExpression()
            };

            while (parser.MatchAndTake(TokenType.Decorator))
                exprs.Add(parser.ParseExpression());

            var isExport = parser.MatchAndTake(TokenType.Export, out var exportToken);

            var stmt = parser.ParseStatement();
            
            Expression result;
            if (stmt is VarExpression varExpr)
            {
                if (varExpr.Declarations.Count != 1)
                    throw new MondCompilerException(stmt, CompilerError.DecoratorCantApplyMultiple);

                var decl = varExpr.Declarations[0];
                var decorator = WrapDecorators(decl.Initializer, exprs);
                var decls = new List<VarExpression.Declaration>
                {
                    new VarExpression.Declaration(decl.Name, decorator),
                };

                if (isExport && !varExpr.IsReadOnly)
                {
                    throw new MondCompilerException(stmt, CompilerError.ExportMustBeFollowedByKeywords);
                }

                result = new VarExpression(token, decls, varExpr.IsReadOnly);
            }
            else
            {
                if (stmt is not FunctionExpression && stmt is not SequenceExpression)
                    throw new MondCompilerException(stmt, CompilerError.DecoratorOnlyOnDeclarations);

                var name = GetName(stmt);
                var decorator = WrapDecorators(stmt, exprs);
                var decls = new List<VarExpression.Declaration>
                {
                    new VarExpression.Declaration(name, decorator),
                };

                result = new VarExpression(token, decls, true);
            }

            if (isExport)
            {
                result.EndToken = parser.Previous;
                return new ExportExpression(exportToken, result);
            }

            return result;
        }

        public static Expression WrapDecorators(Expression stmt, List<Expression> decorators)
        {
            if (decorators == null || decorators.Count == 0)
            {
                return stmt;
            }

            if (stmt is FunctionExpression)
            {
                stmt = MakeAnonymous(stmt);
            }

            var decorator = MakeCallable(decorators.First(), stmt);
            foreach (var expr in decorators.Skip(1))
            {
                decorator = MakeCallable(expr, decorator);
            }
            return decorator;
        }

        private static Expression MakeAnonymous(Expression stmt)
        {
            switch (stmt)
            {
                case SequenceExpression seq:
                    return new SequenceExpression(seq.Token, null, seq.Arguments.ToList(), seq.OtherArguments, seq.Block, seq.Name);

                case FunctionExpression func:
                    return new FunctionExpression(func.Token, null, func.Arguments.ToList(), func.OtherArguments, func.Block, func.Name);

                default: return null;
            }
        }

        private static string GetName(Expression stmt)
        {
            switch (stmt)
            {
                case SequenceExpression seq: return seq.Name;
                case FunctionExpression func: return func.Name;
                default: return null;
            }
        }

        private static Expression MakeCallable(Expression expr, Expression stmt)
        {
            var args = new List<Expression> { stmt };

            switch (expr)
            {
                case CallExpression call:
                    return new CallExpression(call.Token, call.Method, args.Concat(call.Arguments).ToList());

                case FieldExpression _:
                case IdentifierExpression _:
                    return new CallExpression(expr.Token, expr, args);

                default:
                    throw new MondCompilerException(expr, CompilerError.DecoratorMustBeCallable);
            }
        }
    }
}
