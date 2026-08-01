using System.Collections.Generic;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;
using Mond.Compiler.Parselets.Statements;

namespace Mond.Compiler.Parselets
{
    /// <summary>
    /// Parses C#-like switch expressions: <c>subject switch { 1 -&gt; "one", _ -&gt; "other" }</c>
    /// </summary>
    class SwitchExpressionParselet : IInfixParselet
    {
        private const string DiscardName = "_";
        private const string GuardKeyword = "when";

        public int Precedence => (int)PrecedenceValue.Postfix;

        public Expression Parse(Parser parser, Expression left, Token token)
        {
            parser.Take(TokenType.LeftBrace);

            var arms = new List<SwitchValueExpression.Arm>();
            var hasDiscard = false;

            while (!parser.Match(TokenType.RightBrace))
            {
                var armToken = parser.Peek();

                if (hasDiscard)
                    throw new MondCompilerException(armToken, CompilerError.SwitchExprArmAfterDiscard);

                var arm = ParseArm(parser, out var isDiscard);
                arms.Add(arm);
                hasDiscard |= isDiscard;

                if (!parser.MatchAndTake(TokenType.Comma))
                    break;
            }

            parser.Take(TokenType.RightBrace);

            if (!hasDiscard)
                throw new MondCompilerException(token, CompilerError.SwitchExprMissingDiscard);

            return new SwitchValueExpression(token, left, arms);
        }

        private static SwitchValueExpression.Arm ParseArm(Parser parser, out bool isDiscard)
        {
            isDiscard = false;

            var armToken = parser.Peek();

            if (parser.MatchAndTake(TokenType.LeftBrace))
            {
                var fields = VarParselet.ParseObjectDestructuring(parser);
                var pattern = new DestructuredObjectExpression(armToken, fields, null, true);
                var guard = ParseGuard(parser);
                return SwitchValueExpression.Arm.Object(pattern, guard, ParseBody(parser));
            }

            if (parser.MatchAndTake(TokenType.LeftSquare))
            {
                var indices = VarParselet.ParseArrayDestructuring(parser);
                var pattern = new DestructuredArrayExpression(armToken, indices, null, true);
                var guard = ParseGuard(parser);
                return SwitchValueExpression.Arm.Array(pattern, guard, ParseBody(parser));
            }

            // note: we need the var keyword here because otherwise a lone identifier would be treated as a value equality arm, which would prevent the user from binding the value to a variable
            if (parser.MatchAndTake(TokenType.Var))
            {
                var name = parser.Take(TokenType.Identifier);
                var guard = ParseGuard(parser);
                return SwitchValueExpression.Arm.Binding(name.Contents, guard, ParseBody(parser));
            }

            if (IsDiscard(parser.Peek()))
            {
                parser.Take(TokenType.Identifier);
                isDiscard = true;

                if (IsGuard(parser.Peek()))
                    throw new MondCompilerException(armToken, CompilerError.SwitchExprDiscardGuard);

                return SwitchValueExpression.Arm.Discard(ParseBody(parser));
            }

            var conditions = new List<Expression>();
            do
            {
                conditions.Add(ParseArmExpression(parser));
            } while (!parser.Match(TokenType.Pointy) && !IsGuard(parser.Peek()) && parser.MatchAndTake(TokenType.Comma));

            var valuesGuard = ParseGuard(parser);
            return SwitchValueExpression.Arm.Values(conditions, valuesGuard, ParseBody(parser));
        }

        private static Expression ParseGuard(Parser parser)
        {
            if (!IsGuard(parser.Peek()))
                return null;

            parser.Take(TokenType.Identifier);
            return ParseArmExpression(parser);
        }

        /// <summary>
        /// Parses an expression that is terminated by '->'. A lone identifier must be handled
        /// here because <c>ident -&gt;</c> is the lambda shorthand.
        /// </summary>
        private static Expression ParseArmExpression(Parser parser)
        {
            if (parser.Match(TokenType.Identifier) && parser.Match(TokenType.Pointy, 1))
                return new IdentifierExpression(parser.Take(TokenType.Identifier));

            return parser.ParseExpression();
        }

        private static Expression ParseBody(Parser parser)
        {
            parser.Take(TokenType.Pointy);
            return parser.ParseExpression();
        }

        private static bool IsDiscard(Token token) =>
            token.Type == TokenType.Identifier && token.Contents == DiscardName;

        private static bool IsGuard(Token token) =>
            token.Type == TokenType.Identifier && token.Contents == GuardKeyword;
    }
}
