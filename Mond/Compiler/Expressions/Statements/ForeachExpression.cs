using System.Collections.Generic;
using Mond.Compiler.Visitors;

namespace Mond.Compiler.Expressions.Statements
{
    class ForeachExpression : Expression, IStatementExpression
    {
        public Token InToken { get; private set; }
        public string Identifier { get; private set; }
        public Expression Expression { get; private set; }
        public BlockExpression Block { get; private set; }
        public bool IsDestructuring { get { return DestructureExpression != null; } }
        public VarExpression DestructureExpression { get; private set; }

        public bool HasChildren { get { return true; } }

        public ForeachExpression(Token token, Token inToken, string identifier, Expression expression, BlockExpression block, VarExpression destructure = null)
            : base(token)
        {
            InToken = inToken;
            Identifier = identifier;
            Expression = expression;
            Block = block;
            DestructureExpression = destructure;
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);

            var stack = 0;
            var start = context.MakeLabel("foreachStart");
            var cont = context.MakeLabel("foreachContinue");
            var brk = context.MakeLabel("foreachBreak");
            var end = context.MakeLabel("foreachEnd");

            var containsFunction = new LoopContainsFunctionVisitor();
            Block.Accept(containsFunction);

            var enumerator = context.DefineInternal("enumerator", true);

            // set enumerator
            context.Statement(Expression);
            stack += Expression.Compile(context);
            stack += context.LoadField(context.String("getEnumerator"));
            stack += context.Call(0, new List<ImmediateOperand>());
            stack += context.Store(enumerator);

            var loopContext = containsFunction.Value ? new LoopContext(context) : context;

            // loop body
            loopContext.PushScope();
            loopContext.PushLoop(containsFunction.Value ? cont : start, containsFunction.Value ? brk : end);

            var identifier = default(IdentifierOperand);

            if (IsDestructuring)
            {
                identifier = context.DefineInternal(Identifier);
            }
            else
            {
                // create the loop variable outside of the loop context (but inside of its scope!)
                if (!context.DefineIdentifier(Identifier))
                    throw new MondCompilerException(this, CompilerError.IdentifierAlreadyDefined, Identifier);

                identifier = context.Identifier(Identifier);
            }
            stack += loopContext.Bind(start); // continue (without function)

            if (containsFunction.Value)
                stack += loopContext.Enter();

            // loop while moveNext returns true
            context.Statement(InToken, InToken);
            stack += loopContext.Load(enumerator);
            stack += loopContext.LoadField(context.String("moveNext"));
            stack += loopContext.Call(0, new List<ImmediateOperand>());
            stack += loopContext.JumpFalse(containsFunction.Value ? brk : end);

            stack += loopContext.Load(enumerator);
            stack += loopContext.LoadField(context.String("current"));
            stack += loopContext.Store(identifier);

            if (IsDestructuring)
            {
                loopContext.Position(DestructureExpression.Token);
                foreach (var declaration in DestructureExpression.Declarations)
                {
                    if (!loopContext.DefineIdentifier(declaration.Name, DestructureExpression.IsReadOnly))
                        throw new MondCompilerException(declaration.Initializer, CompilerError.IdentifierAlreadyDefined, declaration.Name);

                    var destructureIdent = context.Identifier(declaration.Name);
                    var initializer = declaration.Initializer;
               
                    if (initializer is FieldExpression)
                    {
                        var field = initializer as FieldExpression;

                        stack += loopContext.Load(identifier);
                        loopContext.Position(field.Token);
                        stack += loopContext.LoadField(context.String(field.Name));
                    }
                    else if (initializer is IndexerExpression)
                    {
                        var index = initializer as IndexerExpression;

                        stack += loopContext.Load(identifier);
                        stack += index.Index.Compile(loopContext);
                        loopContext.Position(index.Token);
                        stack += loopContext.LoadArray();
                    }
                    else if (initializer is SliceExpression)
                    {
                        var slice = initializer as SliceExpression;

                        stack += loopContext.Load(identifier);
                        stack += slice.Start.Compile(loopContext);
                        stack += slice.End.Compile(loopContext);



                        stack += loopContext.LoadUndefined();
                        loopContext.Position(slice.Token);
                        stack += loopContext.Slice();
                    }
                    else
                    {
                        throw new System.NotSupportedException();
                    }

                    stack += loopContext.Store(destructureIdent);
                }
            }

            stack += Block.Compile(loopContext);

            if (containsFunction.Value)
            {
                stack += loopContext.Bind(cont); // continue (with function)
                stack += loopContext.Leave();
            }

            stack += loopContext.Jump(start);

            if (containsFunction.Value)
            {
                stack += loopContext.Bind(brk); // break (with function)
                stack += loopContext.Leave();
            }

            loopContext.PopLoop();
            loopContext.PopScope();

            // after loop
            stack += context.Bind(end); // break (without function)
            stack += context.Load(enumerator);
            stack += context.LoadField(context.String("dispose"));
            stack += context.Call(0, new List<ImmediateOperand>());
            stack += context.Drop();

            CheckStack(stack, 0);
            return stack;
        }

        public override Expression Simplify()
        {
            Expression = Expression.Simplify();
            Block = (BlockExpression)Block.Simplify();

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Expression.SetParent(this);
            Block.SetParent(this);
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
