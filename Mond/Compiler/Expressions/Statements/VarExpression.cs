using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mond.Compiler.Expressions.Statements
{
    class VarExpression : Expression
    {
        public class Declaration
        {
            public string Name { get; private set; }
            public Expression Initializer { get; private set; }

            public Declaration(string name, Expression initializer)
            {
                Name = name;
                Initializer = initializer;
            }
        }

        public ReadOnlyCollection<Declaration> Declarations { get; private set; }
        
        public VarExpression(Token token, List<Declaration> declarations)
            : base(token.FileName, token.Line)
        {
            Declarations = declarations.AsReadOnly();
        }

        public override void Print(int indent)
        {
            var indentStr = new string(' ', indent);

            Console.Write(indentStr);
            Console.WriteLine("Var");

            foreach (var declaration in Declarations)
            {
                Console.Write(indentStr);
                Console.WriteLine("-" + declaration.Name + (declaration.Initializer != null ? " =" : ""));

                if (declaration.Initializer != null)
                {
                    declaration.Initializer.Print(indent + 2);
                }
            }
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            var stack = 0;

            foreach (var declaration in Declarations)
            {
                var name = declaration.Name;

                if (!context.DefineIdentifier(name))
                    throw new MondCompilerException(FileName, Line, "Identifier '{0}' was previously defined in this scope", name);

                if (declaration.Initializer != null)
                {
                    var identifier = context.Identifier(name);

                    stack += declaration.Initializer.Compile(context);
                    stack += context.Store(identifier);
                }
            }

            CheckStack(stack, 0);
            return stack;
        }

        public override Expression Simplify()
        {
            Declarations = Declarations
                .Select(d => new Declaration(d.Name, d.Initializer == null ? null : d.Initializer.Simplify()))
                .ToList()
                .AsReadOnly();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            foreach (var declaration in Declarations.Where(d => d.Initializer != null))
            {
                declaration.Initializer.SetParent(this);
            }
        }
    }
}
