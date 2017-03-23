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
            if (!(stmt is FunctionExpression) && !(stmt is SequenceExpression))
                throw new MondCompilerException(stmt, CompilerError.DecoratorOnlyOnFuncs);

            if (IsOperator(stmt))
                throw new MondCompilerException(stmt, CompilerError.NoDecoratorsOnOperatos);

            var name = GetName(stmt);
            stmt = MakeAnonymous(stmt);
            
            var decorator = MakeCallable(exprs.First(), stmt);
            foreach (var expr in exprs.Skip(1))
                decorator = MakeCallable(expr, decorator);

            var decls = new List<VarExpression.Declaration>
            {
                new VarExpression.Declaration(name, decorator)
            };

            return new VarExpression(token, decls, true);
        }

        private static Expression MakeAnonymous(Expression stmt)
        {
            if (stmt is SequenceExpression)
            {
                var seq = (SequenceExpression)stmt;
                return new SequenceExpression(seq.Token, null, seq.Arguments.ToList(), seq.OtherArguments, false, seq.Block, seq.Name);
            }
            else if (stmt is FunctionExpression)
            {
                var func = (FunctionExpression)stmt;
                return new FunctionExpression(func.Token, null, func.Arguments.ToList(), func.OtherArguments, false, func.Block, func.Name);
            }
            else
                return null;
        }

        private static bool IsOperator(Expression stmt)
        {
            if (stmt is FunctionExpression)
                return ((FunctionExpression)stmt).IsOperator;
            else if (stmt is SequenceExpression)
                return ((SequenceExpression)stmt).IsOperator;
            else
                return false;
        }

        private static string GetName(Expression stmt)
        {
            if (stmt is FunctionExpression)
                return ((FunctionExpression)stmt).Name;
            else if(stmt is SequenceExpression)
                return ((SequenceExpression)stmt).Name;
            else
                return null;
        }

        private static Expression MakeCallable(Expression expr, Expression stmt)
        {
            var args = new List<Expression> { stmt };
            if (expr is CallExpression)
            {
                var call = (CallExpression)expr;
                return new CallExpression(call.Token, call.Method, args.Concat(call.Arguments).ToList());
            }
            else if (expr is FieldExpression || expr is IdentifierExpression)
            {
                return new CallExpression(expr.Token, expr, args);
            }
            else
            {
                throw new MondCompilerException(expr, CompilerError.DecoratorMustBeCallable);
            }
        }
    }
}
