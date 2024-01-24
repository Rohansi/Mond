using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mond.SourceGenerator
{
    internal class SyntaxReceiver : ISyntaxContextReceiver
    {
        public List<INamedTypeSymbol> Modules { get; } = new List<INamedTypeSymbol>();
        public List<INamedTypeSymbol> Classes { get; } = new List<INamedTypeSymbol>();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is ClassDeclarationSyntax classDecl)
            {
                var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl);
                if (!(symbol is INamedTypeSymbol classSymbol))
                {
                    // todo: can we log somewhere?
                    return;
                }

                var attributes = classSymbol.GetAttributes();
                var isModule = HasAttribute(attributes, "MondModuleAttribute");
                var isClass = HasAttribute(attributes, "MondClassAttribute");
                
                if (isModule)
                {
                    Modules.Add(classSymbol);
                }
                else if (isClass)
                {
                    Classes.Add(classSymbol);
                }
            }
        }

        private static bool HasAttribute(ImmutableArray<AttributeData> attributes, string name)
        {
            return attributes.Any(a =>
                a.AttributeClass != null &&
                a.AttributeClass.ContainingNamespace?.Name == "Mond.Binding" &&
                a.AttributeClass.Name == name);
        }
    }
}
