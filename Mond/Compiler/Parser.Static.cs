using System.Collections.Generic;
using Mond.Compiler.Parselets;
using Mond.Compiler.Parselets.Statements;

namespace Mond.Compiler
{
    partial class Parser
    {
        private static Dictionary<TokenType, IPrefixParselet> _prefixParselets;
        private static Dictionary<TokenType, IInfixParselet> _infixParselets;
        private static Dictionary<TokenType, IStatementParselet> _statementParselets;

        static Parser()
        {
            _prefixParselets = new Dictionary<TokenType, IPrefixParselet>();
            _infixParselets = new Dictionary<TokenType, IInfixParselet>();
            _statementParselets = new Dictionary<TokenType, IStatementParselet>();

            // leaves
            RegisterPrefix(TokenType.Number, new NumberParselet());
            RegisterPrefix(TokenType.String, new StringParselet());
            RegisterPrefix(TokenType.Identifier, new IdentifierParselet());

            RegisterPrefix(TokenType.Global, new GlobalParselet());
            RegisterPrefix(TokenType.Undefined, new UndefinedParselet());
            RegisterPrefix(TokenType.Null, new NullParselet());
            RegisterPrefix(TokenType.True, new BoolParselet(true));
            RegisterPrefix(TokenType.False, new BoolParselet(false));
            RegisterPrefix(TokenType.NaN, new NaNParselet());
            RegisterPrefix(TokenType.Infinity, new InfinityParselet());

            // math operations
            RegisterInfix(TokenType.Add, new BinaryOperatorParselet((int)PrecedenceValue.Addition, false));
            RegisterInfix(TokenType.Subtract, new BinaryOperatorParselet((int)PrecedenceValue.Addition, false));
            RegisterInfix(TokenType.Multiply, new BinaryOperatorParselet((int)PrecedenceValue.Multiplication, false));
            RegisterInfix(TokenType.Divide, new BinaryOperatorParselet((int)PrecedenceValue.Multiplication, false));
            RegisterInfix(TokenType.Modulo, new BinaryOperatorParselet((int)PrecedenceValue.Multiplication, false));
            RegisterInfix(TokenType.Exponent, new BinaryOperatorParselet((int)PrecedenceValue.Multiplication, false));
            RegisterInfix(TokenType.BitLeftShift, new BinaryOperatorParselet((int)PrecedenceValue.BitShift, false));
            RegisterInfix(TokenType.BitRightShift, new BinaryOperatorParselet((int)PrecedenceValue.BitShift, false));
            RegisterInfix(TokenType.BitAnd, new BinaryOperatorParselet((int)PrecedenceValue.BitAnd, false));
            RegisterInfix(TokenType.BitOr, new BinaryOperatorParselet((int)PrecedenceValue.BitOr, false));
            RegisterInfix(TokenType.BitXor, new BinaryOperatorParselet((int)PrecedenceValue.BitXor, false));
            RegisterPrefix(TokenType.Subtract, new PrefixOperatorParselet((int)PrecedenceValue.Prefix));
            RegisterPrefix(TokenType.BitNot, new PrefixOperatorParselet((int)PrecedenceValue.Prefix));

            // conditional operations
            RegisterPrefix(TokenType.Not, new PrefixOperatorParselet((int)PrecedenceValue.Prefix));
            RegisterInfix(TokenType.ConditionalAnd, new BinaryOperatorParselet((int)PrecedenceValue.ConditionalAnd, false));
            RegisterInfix(TokenType.ConditionalOr, new BinaryOperatorParselet((int)PrecedenceValue.ConditionalOr, false));

            // relational operations
            RegisterInfix(TokenType.EqualTo, new BinaryOperatorParselet((int)PrecedenceValue.Equality, false));
            RegisterInfix(TokenType.NotEqualTo, new BinaryOperatorParselet((int)PrecedenceValue.Equality, false));
            RegisterInfix(TokenType.GreaterThan, new BinaryOperatorParselet((int)PrecedenceValue.Relational, false));
            RegisterInfix(TokenType.GreaterThanOrEqual, new BinaryOperatorParselet((int)PrecedenceValue.Relational, false));
            RegisterInfix(TokenType.LessThan, new BinaryOperatorParselet((int)PrecedenceValue.Relational, false));
            RegisterInfix(TokenType.LessThanOrEqual, new BinaryOperatorParselet((int)PrecedenceValue.Relational, false));
            RegisterInfix(TokenType.In, new BinaryOperatorParselet((int)PrecedenceValue.Relational, false));

            // prefix inc/decrement
            RegisterPrefix(TokenType.Increment, new PrefixOperatorParselet((int)PrecedenceValue.Prefix));
            RegisterPrefix(TokenType.Decrement, new PrefixOperatorParselet((int)PrecedenceValue.Prefix));

            //postfix inc/decrement
            RegisterInfix(TokenType.Increment, new PostfixOperatorParselet((int)PrecedenceValue.Postfix));
            RegisterInfix(TokenType.Decrement, new PostfixOperatorParselet((int)PrecedenceValue.Postfix));

            // assignment
            RegisterInfix(TokenType.Assign, new BinaryOperatorParselet((int)PrecedenceValue.Assign, true));
            RegisterInfix(TokenType.AddAssign, new BinaryOperatorParselet((int)PrecedenceValue.Assign, true));
            RegisterInfix(TokenType.SubtractAssign, new BinaryOperatorParselet((int)PrecedenceValue.Assign, true));
            RegisterInfix(TokenType.MultiplyAssign, new BinaryOperatorParselet((int)PrecedenceValue.Assign, true));
            RegisterInfix(TokenType.DivideAssign, new BinaryOperatorParselet((int)PrecedenceValue.Assign, true));
            RegisterInfix(TokenType.ModuloAssign, new BinaryOperatorParselet((int)PrecedenceValue.Assign, true));
            RegisterInfix(TokenType.BitLeftShiftAssign, new BinaryOperatorParselet((int)PrecedenceValue.Assign, true));
            RegisterInfix(TokenType.BitRightShiftAssign, new BinaryOperatorParselet((int)PrecedenceValue.Assign, true));
            RegisterInfix(TokenType.BitAndAssign, new BinaryOperatorParselet((int)PrecedenceValue.Assign, true));
            RegisterInfix(TokenType.BitOrAssign, new BinaryOperatorParselet((int)PrecedenceValue.Assign, true));
            RegisterInfix(TokenType.BitXorAssign, new BinaryOperatorParselet((int)PrecedenceValue.Assign, true));

            // other expression stuff
            RegisterPrefix(TokenType.LeftParen, new GroupParselet());
            RegisterInfix(TokenType.LeftParen, new CallParselet());
            RegisterInfix(TokenType.QuestionMark, new ConditionalParselet());
            RegisterInfix(TokenType.Dot, new FieldParselet());
            RegisterInfix(TokenType.LeftSquare, new IndexerParselet());
            RegisterPrefix(TokenType.LeftBrace, new ObjectParselet());
            RegisterPrefix(TokenType.LeftSquare, new ArrayParselet());
            RegisterPrefix(TokenType.Fun, new FunctionParselet());
            RegisterPrefix(TokenType.Seq, new SequenceParselet());
            RegisterInfix(TokenType.Pipeline, new PipelineParselet());

            // statements
            RegisterStatement(TokenType.Semicolon, new SemicolonParselet());
            RegisterStatement(TokenType.LeftBrace, new ScopeParselet());
            RegisterStatement(TokenType.Fun, new FunctionParselet());
            RegisterStatement(TokenType.Return, new ReturnParselet());
            RegisterStatement(TokenType.Seq, new SequenceParselet());
            RegisterStatement(TokenType.Yield, new YieldParselet());
            RegisterStatement(TokenType.Break, new BreakParselet());
            RegisterStatement(TokenType.Continue, new ContinueParselet());
            RegisterStatement(TokenType.Var, new VarParselet(false));
            RegisterStatement(TokenType.Const, new VarParselet(true));
            RegisterStatement(TokenType.If, new IfParselet());
            RegisterStatement(TokenType.While, new WhileParselet());
            RegisterStatement(TokenType.Do, new DoWhileParselet());
            RegisterStatement(TokenType.For, new ForParselet());
            RegisterStatement(TokenType.Foreach, new ForeachParselet());
            RegisterStatement(TokenType.Switch, new SwitchParselet());
        }

        static void RegisterPrefix(TokenType type, IPrefixParselet parselet)
        {
            _prefixParselets.Add(type, parselet);
        }

        static void RegisterInfix(TokenType type, IInfixParselet parselet)
        {
            _infixParselets.Add(type, parselet);
        }

        static void RegisterStatement(TokenType type, IStatementParselet parselet)
        {
            _statementParselets.Add(type, parselet);
        }
    }
}
