using System.Collections.Generic;

namespace Mond.Compiler.Expressions
{
    internal interface IDeclarationExpression
    {
        IEnumerable<string> DeclaredIdentifiers { get; }
    }
}
