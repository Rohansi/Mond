using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Mond.Compiler.Expressions.Statements
{
    class FunctionExpression : Expression, IBlockStatementExpression
    {
        public string Name { get; private set; }
        public ReadOnlyCollection<string> Arguments { get; private set; }
        public BlockExpression Block { get; private set; }

        public FunctionExpression(Token token, string name, List<string> arguments, BlockExpression block)
            : base(token.FileName, token.Line)
        {
            Name = name;
            Arguments = arguments.AsReadOnly();
            Block = block;
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

        public override int Compile(CompilerContext context)
        {
            context.Line(FileName, Line);

            IdentifierOperand identifier = null;
            var global = context.FrameIndex == 0;

            if (Name != null)
            {
                if (!global)
                {
                    context.DefineIdentifier(Name);
                    identifier = context.Identifier(Name);
                }
            }

            var label = context.Label("func");
            var functionContext = context.MakeContext();
            functionContext.Function(FileName, Name);

            context.PushFrame();

            for (var i = 0; i < Arguments.Count; i++)
            {
                var name = Arguments[i];

                if (!functionContext.DefineArgument(i, name))
                    throw new MondCompilerException(FileName, Line, "Identifier '{0}' was previously defined in this scope", name);
            }

            functionContext.Bind(label);
            functionContext.Enter();
            Block.Compile(functionContext);
            functionContext.LoadUndefined();
            functionContext.Return();

            context.PopFrame();

            context.Closure(label);

            if (Name != null)
            {
                if (!global)
                {
                    context.Store(identifier);
                }
                else
                {
                    context.LoadGlobal();
                    context.StoreField(context.String(Name));
                }

                return 0;
            }

            return 1;
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
