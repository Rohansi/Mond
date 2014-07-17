using System;

namespace Mond.Compiler.Expressions
{
    class IdentifierExpression : Expression, IStorableExpression
    {
        public string Name { get; private set; }

        public IdentifierExpression(Token token)
            : base(token.FileName, token.Line)
        {
            Name = token.Contents;
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("identifier: {0}", Name);
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var identifier = context.Identifier(Name);

            /*if (identifier == null)
                throw new MondCompilerException(FileName, Line, "Undefined identifier '{0}'", Name);*/

            if (identifier == null)
            {
                context.LoadGlobal();
                context.LoadField(context.String(Name));
            }
            else
            {
                context.Load(identifier);
            }

            return 1;
        }

        public int CompileStore(FunctionContext context)
        {
            var stack = 0;
            var identifier = context.Identifier(Name);

            if (identifier == null)
            {
                stack += context.LoadGlobal();
                stack += context.StoreField(context.String(Name));
            }
            else
            {
                if (identifier.IsReadOnly)
                    throw new MondCompilerException(FileName, Line, CompilerError.CantModifyReadonlyVar, Name);

                stack += context.Store(identifier);
            }

            return stack;
        }

        public override Expression Simplify()
        {
            return this;
        }
    }
}
