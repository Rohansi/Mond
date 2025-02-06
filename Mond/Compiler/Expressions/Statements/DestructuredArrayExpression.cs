using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Mond.Compiler.Expressions.Statements
{
    class DestructuredArrayExpression : Expression, IStatementExpression
    {
        public class Index
        {
            public string Name { get; }
            public bool IsSlice { get; }

            public Index(string name, bool isSlice)
            {
                Name = name;
                IsSlice = isSlice;
            }
        }

        public ReadOnlyCollection<Index> Indices { get; }
        public Expression Initializer { get; private set; }
        public bool IsReadOnly { get; }
        public bool HasChildren => false;

        public DestructuredArrayExpression(Token token, IList<Index> indices, Expression initializer, bool isReadOnly)
            : base(token)
        {
            Indices = new ReadOnlyCollection<Index>(indices);
            Initializer = initializer;
            IsReadOnly = isReadOnly;
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);
            
            var stack = Initializer?.Compile(context) ?? 1;
            var global = context.ArgIndex == 0 && context.Compiler.Options.MakeRootDeclarationsGlobal;

            var hasSlice = false;
            var headSize = 0;
            var tailSize = 0;

            foreach (var index in Indices)
            {
                if (index.IsSlice)
                {
                    if (hasSlice)
                        throw new InvalidOperationException($"Multiple slices in {nameof(DestructuredArrayExpression)}");

                    hasSlice = true;
                }
                else if (hasSlice)
                {
                    tailSize++;
                }
                else
                {
                    headSize++;
                }
            }

            var fixedSize = headSize + tailSize;
            
            stack += context.Dup();
            stack += context.InstanceCall(context.String("length"), 0, new List<ImmediateOperand>());

            var inTail = false;
            var fixedI = 0;
            for (var i = 0; i < Indices.Count; i++)
            {
                var index = Indices[i];
                var assign = context.MakeLabel("arrayDestructureAssign");
                var destruct = context.MakeLabel("arrayDestructureIndex");

                if (index.IsSlice)
                    inTail = true;

                if (i < Indices.Count - 1)
                    stack += context.Dup2(); // -> init.length(), init

                stack += context.Load(context.Number(index.IsSlice ? fixedSize : fixedI));
                stack += context.BinaryOperation(TokenType.GreaterThan);
                stack += context.JumpTrue(destruct);
                stack += context.Drop(); // drops initializer
                stack += index.IsSlice ? context.NewArray(0) : context.LoadUndefined();
                stack += context.Jump(assign);

                stack += context.Bind(destruct);

                if (index.IsSlice)
                {
                    stack += context.Load(context.Number(fixedI));
                    stack += context.Load(context.Number(-tailSize - 1));
                    stack += context.LoadUndefined();
                    stack += context.Slice();
                }
                else
                {
                    stack += context.Load(context.Number(inTail ? fixedI - fixedSize : fixedI));
                    stack += context.LoadArray();
                }

                stack += context.Bind(assign);

                if (global)
                {
                    stack += context.LoadGlobal();
                    stack += context.StoreField(context.String(index.Name));
                }
                else
                {
                    if (!context.DefineIdentifier(index.Name, IsReadOnly))
                        throw new MondCompilerException(this, CompilerError.IdentifierAlreadyDefined, index.Name);

                    stack += context.Store(context.Identifier(index.Name));
                }

                if (!index.IsSlice)
                    fixedI++;
            }

            CheckStack(stack, 0);
            return -1;
        }

        public override Expression Simplify()
        {
            Initializer = Initializer?.Simplify();
            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Initializer?.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
