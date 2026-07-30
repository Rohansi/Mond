using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mond.SourceGenerator;

internal sealed class BindingCandidate
{
    public INamedTypeSymbol Symbol { get; }
    public bool IsPrototype { get; }
    public bool IsModule { get; }
    public bool IsClass { get; }
    public Location MissingPartialLocation { get; }

    private BindingCandidate(
        INamedTypeSymbol symbol,
        bool isPrototype,
        bool isModule,
        bool isClass,
        Location missingPartialLocation)
    {
        Symbol = symbol;
        IsPrototype = isPrototype;
        IsModule = isModule;
        IsClass = isClass;
        MissingPartialLocation = missingPartialLocation;
    }

    public static BindingCandidate TryCreate(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDecl)
        {
            return null;
        }

        var symbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, classDecl);
        if (!(symbol is INamedTypeSymbol classSymbol))
        {
            // todo: can we log somewhere?
            return null;
        }

        var attributes = classSymbol.GetAttributes();
        var isPrototype = attributes.HasAttribute("MondPrototypeAttribute");
        var isModule = attributes.HasAttribute("MondModuleAttribute");
        var isClass = attributes.HasAttribute("MondClassAttribute");

        if (!isPrototype && !isModule && !isClass)
        {
            return null;
        }

        var missingPartialLocation = IsMissingPartial(classDecl)
            ? classDecl.Identifier.GetLocation()
            : null;

        return new BindingCandidate(classSymbol, isPrototype, isModule, isClass, missingPartialLocation);

        static bool IsMissingPartial(ClassDeclarationSyntax klass)
        {
            while (klass != null)
            {
                if (!klass.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return true;
                }

                klass = klass.Parent as ClassDeclarationSyntax;
            }

            return false;
        }
    }
}
