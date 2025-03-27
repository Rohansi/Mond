using System;
using System.Collections.Generic;

namespace Mond.Compiler.Expressions.Statements
{
    internal class DeclareGlobalsExpression : Expression, IStatementExpression, IDeclarationExpression
    {
        private readonly List<string> _names;
        
        public bool HasChildren => false;

        public IEnumerable<string> DeclaredIdentifiers => _names;

        public DeclareGlobalsExpression(Token token, List<string> names)
            : base(token)
        {
            _names = names ?? throw new ArgumentNullException(nameof(names));
        }

        public override int Compile(FunctionContext context)
        {
            return 0;
        }

        public override Expression Simplify(SimplifyContext context)
        {
            foreach (var name in _names)
            {
                if (!context.DefineGlobal(name))
                {
                    throw new MondCompilerException(this, CompilerError.IdentifierAlreadyDefined, name);
                }
            }

            return this;
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
