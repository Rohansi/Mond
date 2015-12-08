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
            public string Name { get; private set; }
            public bool IsSlice { get; private set; }

            public Index(string name, bool isSlice)
            {
                Name = name;
                IsSlice = isSlice;
            }
        }

        public ReadOnlyCollection<Index> Indecies { get; private set; }
        public Expression Initializer { get; private set; }
        public bool IsReadOnly { get; private set; }
        public bool HasChildren { get { return false; } }

        public DestructuredArrayExpression(Token token, IList<Index> indecies, Expression initializer, bool isReadOnly)
            : base(token)
        {
            Indecies = new ReadOnlyCollection<Index>(indecies);
            Initializer = initializer;
            IsReadOnly = isReadOnly;
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);

            var i = 0;
            var startIndex = 0;
            var stack = Initializer == null ? 1 : Initializer.Compile(context);
            var global = context.ArgIndex == 0 && context.Compiler.Options.MakeRootDeclarationsGlobal;

            foreach (var index in Indecies)
            {
                var assign = context.MakeLabel("arrayDestructureAssign");
                var destruct = context.MakeLabel("arrayDestructureIndex");
                var remaining = Indecies.Skip(i + 1).Count();
               
                stack += context.Dup();
                stack += context.Dup();
                stack += context.LoadField(context.String("length"));
                stack += context.Call(0, new List<ImmediateOperand>());
                stack += context.Load(context.Number(1));
                stack += context.BinaryOperation(TokenType.Subtract);

                if (index.IsSlice)
                {
                    stack += context.Load(context.Number(Math.Abs(startIndex)));
                    stack += context.BinaryOperation(TokenType.Subtract);
                    stack += context.Load(context.Number(remaining));
                }
                else
                {
                    stack += context.Load(context.Number(Math.Abs(startIndex)));
                }

                stack += context.BinaryOperation( TokenType.GreaterThanOrEqual );
                stack += context.JumpTrue( destruct );
                stack += context.Drop();
                stack += index.IsSlice ? context.NewArray(0) : context.LoadUndefined();
                stack += context.Jump(assign);

                stack += context.Bind(destruct);
                stack += context.Load(context.Number(startIndex));

                if (index.IsSlice)
                {
                    startIndex = -remaining;

                    stack += context.Load(context.Number(startIndex - 1));
                    stack += context.LoadUndefined();
                    stack += context.Slice();
                }
                else
                {
                    stack += context.LoadArray();
                    startIndex++;
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

                i++;
            }

            stack += context.Drop();
            
            CheckStack(stack, 0);
            return -1;
        }

        public override Expression Simplify()
        {
            if (Initializer != null)
                Initializer = Initializer.Simplify();

            return this;
        }
    }
}
