﻿using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mond.SourceGenerator;

internal class SyntaxReceiver : ISyntaxContextReceiver
{
    public HashSet<INamedTypeSymbol> Prototypes { get; } = new(SymbolEqualityComparer.Default);
    public HashSet<INamedTypeSymbol> Modules { get; } = new(SymbolEqualityComparer.Default);
    public HashSet<INamedTypeSymbol> Classes { get; } = new(SymbolEqualityComparer.Default);
    public HashSet<Location> MissingPartials { get; } = new();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDecl)
        {
            return;
        }
        
        var symbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, classDecl);
        if (!(symbol is INamedTypeSymbol classSymbol))
        {
            // todo: can we log somewhere?
            return;
        }

        var attributes = classSymbol.GetAttributes();
        var isPrototype = attributes.HasAttribute("MondPrototypeAttribute");
        var isModule = attributes.HasAttribute("MondModuleAttribute");
        var isClass = attributes.HasAttribute("MondClassAttribute");

        if (!isPrototype && !isModule && !isClass)
        {
            return;
        }

        if (IsMissingPartial(classDecl))
        {
            MissingPartials.Add(classDecl.Identifier.GetLocation());
        }

        if (isPrototype)
        {
            Prototypes.Add(classSymbol);
        }

        if (isModule)
        {
            Modules.Add(classSymbol);
        }

        if (isClass)
        {
            Classes.Add(classSymbol);
        }

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
