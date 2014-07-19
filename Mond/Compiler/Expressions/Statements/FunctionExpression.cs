using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Mond.Compiler.Expressions.Statements
{
    class FunctionExpression : Expression, IStatementExpression
    {
        public string Name { get; private set; }
        public ReadOnlyCollection<string> Arguments { get; private set; }
        public BlockExpression Block { get; private set; }

        public string DebugName { get; private set; }

        public FunctionExpression(Token token, string name, List<string> arguments, BlockExpression block, string debugName = null)
            : base(token.FileName, token.Line)
        {
            Name = name;
            Arguments = arguments.AsReadOnly();
            Block = block;

            DebugName = debugName;
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("Function " + Name);

            Console.Write(indentStr);
            Console.Write("-Arguments: ");
            Console.WriteLine(string.Join(", ", Arguments));

            Console.Write(indentStr);
            Console.WriteLine("-Block");
            Block.Print(indent + 2);
        }

        public virtual void CompileBody(FunctionContext context)
        {
            var stack = 0;

            stack += context.Bind(context.Label);
            stack += context.Enter();
            stack += Block.Compile(context);
            stack += context.LoadUndefined();
            stack += context.Return();

            CheckStack(stack, 0);
        }

        public override int Compile(FunctionContext context)
        {
            var isStatement = Parent is IBlockExpression;

            if (Name == null && isStatement)
                throw new MondCompilerException(FileName, Line, CompilerError.FunctionNeverUsed);

            IdentifierOperand identifier = null;

            if (Name != null)
            {
                if (!context.DefineIdentifier(Name, true))
                    throw new MondCompilerException(FileName, Line, CompilerError.IdentifierAlreadyDefined, Name);

                identifier = context.Identifier(Name);
            }

            // compile body
            var functionContext = context.MakeFunction(Name ?? DebugName);
            functionContext.Function(functionContext.FullName);
            functionContext.Line(FileName, Line);
            functionContext.PushScope();

            for (var i = 0; i < Arguments.Count; i++)
            {
                var name = Arguments[i];

                if (!functionContext.DefineArgument(i, name))
                    throw new MondCompilerException(FileName, Line, CompilerError.IdentifierAlreadyDefined, name);
            }

            CompileBody(functionContext);
            functionContext.PopScope();

            // assign result
            var stack = 0;
            stack += context.Closure(functionContext.Label);

            if (identifier != null)
            {
                if (!isStatement) // statements should leave nothing on the stack
                    stack += context.Dup();

                stack += context.Store(identifier);

                if (isStatement)
                {
                    CheckStack(stack, 0);
                    return stack;
                }
            }

            CheckStack(stack, 1);
            return stack;
        }

        public override Expression Simplify()
        {
            Block = (BlockExpression)Block.Simplify();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Block.SetParent(this);
        }
    }
}
