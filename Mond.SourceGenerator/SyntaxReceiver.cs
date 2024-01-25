using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mond.SourceGenerator;

internal class SyntaxReceiver : ISyntaxContextReceiver
{
    public HashSet<INamedTypeSymbol> Modules { get; } = new(SymbolEqualityComparer.Default);
    public HashSet<INamedTypeSymbol> Classes { get; } = new(SymbolEqualityComparer.Default);
    public HashSet<Location> MissingPartials { get; } = new();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is ClassDeclarationSyntax classDecl)
        {
            var symbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, classDecl);
            if (!(symbol is INamedTypeSymbol classSymbol))
            {
                // todo: can we log somewhere?
                return;
            }

            var attributes = classSymbol.GetAttributes();
            var isModule = attributes.HasAttribute("MondModuleAttribute");
            var isClass = attributes.HasAttribute("MondClassAttribute");

            if (!isModule && !isClass)
            {
                return;
            }

            if (!classDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                MissingPartials.Add(classDecl.Identifier.GetLocation());
            }

            if (isModule)
            {
                Modules.Add(classSymbol);
            }

            if (isClass)
            {
                Classes.Add(classSymbol);
            }
        }
    }
}
