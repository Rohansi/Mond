using System;

namespace Mond.Compiler.Expressions.Statements
{
    internal class ExportAllExpression : Expression, IStatementExpression
    {
        public string ModuleName { get; }

        public bool HasChildren => false;

        public ExportAllExpression(Token token, string moduleName)
            : base(token)
        {
            ModuleName = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
        }

        public override int Compile(FunctionContext context)
        {
            var require = context.Identifier("require");
            if (require == null && !context.Compiler.Options.UseImplicitGlobals)
            {
                throw new MondCompilerException(this, CompilerError.ImportMissingRequire);
            }

            if (!context.TryGetIdentifier("exports", out var exportsOperand))
            {
                throw new MondCompilerException(this, CompilerError.ExportCannotBeUsedOutsideModule);
            }
            
            if (exportsOperand is not ArgumentIdentifierOperand || exportsOperand.FrameIndex != context.FrameDepth)
            {
                throw new MondCompilerException(this, CompilerError.ExportCannotBeUsedOutsideModule);
            }

            if (Parent is not ScopeExpression { Parent: FunctionExpression moduleFunction } ||
                moduleFunction.Arguments.Count != 1 || moduleFunction.Arguments[0] != "exports")
            {
                throw new MondCompilerException(this, CompilerError.ExportOnlyOnTopLevelDeclarations);
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
            
            var start = context.MakeLabel("loopStart");
            var end = context.MakeLabel("loopEnd");

            var enumerator = context.DefineInternal("enumerator", true);

            // enumerator = require(moduleName).getEnumerator() (note: require is above)
            stack += context.InstanceCall(context.String("getEnumerator"), 0, []);
            stack += context.Store(enumerator);

            // if (!enumerator.moveNext()) goto end
            stack += context.Bind(start);
            stack += context.Load(enumerator);
            stack += context.InstanceCall(context.String("moveNext"), 0, []);
            stack += context.JumpFalse(end);

            // exports.add(current.key, current.value)
            stack += context.Load(exportsOperand);
            stack += context.Load(enumerator);
            stack += context.LoadField(context.String("current"));
            stack += context.Dup();
            stack += context.LoadField(context.String("key"));
            stack += context.Swap();
            stack += context.LoadField(context.String("value"));
            stack += context.InstanceCall(context.String("add"), 2, []);
            stack += context.Drop(); // ignore return value

            // goto start
            stack += context.Jump(start);

            // end: enumerator.dispose()
            stack += context.Bind(end);
            stack += context.Load(enumerator);
            stack += context.InstanceCall(context.String("dispose"), 0, []);
            stack += context.Drop();

            CheckStack(stack, 0);
            return stack;
        }

        public override Expression Simplify(SimplifyContext context)
        {
            var require = context.Identifier("require");
            if (require == null && !context.Compiler.Options.UseImplicitGlobals)
            {
                throw new MondCompilerException(this, CompilerError.ImportMissingRequire);
            }

            if (require != null)
            {
                context.ReferenceIdentifier(require);
            }

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
