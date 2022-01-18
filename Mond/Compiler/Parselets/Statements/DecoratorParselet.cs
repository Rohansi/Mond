using System.Collections.Generic;
using System.Linq;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    class DecoratorParselet : IStatementParselet
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

            var stmt = parser.ParseStatement(trailingSemicolon);

            if (stmt is VarExpression varExpr)
            {
                if (varExpr.Declarations.Count != 1)
                    throw new MondCompilerException(stmt, CompilerError.DecoratorCantApplyMultiple);

                var decl = varExpr.Declarations[0];
                stmt = decl.Initializer;

                var decorator = MakeCallable(exprs.First(), stmt);
                foreach (var expr in exprs.Skip(1))
                    decorator = MakeCallable(expr, decorator);

                var decls = new List<VarExpression.Declaration>
                {
                    new VarExpression.Declaration(decl.Name, decorator),
                };

                return new VarExpression(token, decls, varExpr.IsReadOnly);
            }
            else
            {
                if (stmt is not FunctionExpression && stmt is not SequenceExpression)
                    throw new MondCompilerException(stmt, CompilerError.DecoratorOnlyOnDeclarations);

                var name = GetName(stmt);
                stmt = MakeAnonymous(stmt);

                var decorator = MakeCallable(exprs.First(), stmt);
                foreach (var expr in exprs.Skip(1))
                    decorator = MakeCallable(expr, decorator);

                var decls = new List<VarExpression.Declaration>
                {
                    new VarExpression.Declaration(name, decorator),
                };

                return new VarExpression(token, decls, true);
            }
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
