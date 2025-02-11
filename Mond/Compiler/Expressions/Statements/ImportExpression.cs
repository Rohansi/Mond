using System;
using System.Collections.Generic;

namespace Mond.Compiler.Expressions.Statements
{
    internal class ImportExpression : Expression, IStatementExpression
    {
        public string ModuleName { get; }
        public string BindName { get; }
        public List<DestructuredObjectExpression.Field> Fields { get; }

        public bool HasChildren { get; }

        public ImportExpression(Token token, string moduleName, string bindName)
            : base(token)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
                throw new ArgumentException(nameof(moduleName));
            if (string.IsNullOrWhiteSpace(bindName))
                throw new ArgumentException(nameof(bindName));

            ModuleName = moduleName;
            BindName = bindName;
            Fields = [];
        }

        public ImportExpression(Token token, string moduleName, List<DestructuredObjectExpression.Field> fields)
            : base(token)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
                throw new ArgumentException(nameof(moduleName));

            ModuleName = moduleName;
            BindName = null;
            Fields = fields ?? [];
        }

        public override int Compile(FunctionContext context)
        {
            var require = context.Identifier("require");
            if (require == null && !context.Compiler.Options.UseImplicitGlobals)
            {
                throw new MondCompilerException(this, CompilerError.ImportMissingRequire);
            }

            context.Position(Token);

            var stack = 0;
            if (require == null)
            {
                stack += context.LoadGlobal();
                stack += context.Load(context.String("require"));
            }
            else
            {
                stack += context.Load(require);
            }

            stack += context.Load(context.String(ModuleName));
            stack += context.Call(1, []);

            CheckStack(stack, 1);

            if (BindName != null)
            {
                if (context.Compiler.Options.MakeRootDeclarationsGlobal)
                {
                    stack += context.LoadGlobal();
                    stack += context.StoreField(context.String(BindName));
                }
                else
                {
                    if (!context.DefineIdentifier(BindName, true))
                    {
                        throw new MondCompilerException(this, CompilerError.IdentifierAlreadyDefined, BindName);
                    }

                    stack += context.Store(context.Identifier(BindName));
                }
            }
            else
            {
                stack += new DestructuredObjectExpression(Token, Fields, null, true).Compile(context);
            }

            CheckStack(stack, 0);
            return stack;
        }

        public override Expression Simplify()
        {
            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
