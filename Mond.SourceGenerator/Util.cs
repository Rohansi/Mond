using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Mond.SourceGenerator
{
    internal static class Util
    {
        public static bool HasAttribute(this ISymbol symbol, string name) =>
            symbol.GetAttributes().HasAttribute(name);

        public static bool HasAttribute(this ImmutableArray<AttributeData> attributes, string name) =>
            attributes.TryGetAttribute(name, out _);

        public static AttributeData? GetAttribute(this ISymbol symbol, string name) =>
            symbol.GetAttributes().GetAttribute(name);

        public static AttributeData? GetAttribute(this ImmutableArray<AttributeData> attributes, string name) =>
            attributes.TryGetAttribute(name, out var value) ? value : null;

        public static bool TryGetAttribute(this ISymbol symbol, string name, out AttributeData attribute) =>
            symbol.GetAttributes().TryGetAttribute(name, out attribute);

        public static bool TryGetAttribute(this ImmutableArray<AttributeData> attributes, string name, out AttributeData attribute)
        {
            attribute = attributes.SingleOrDefault(a =>
                a.AttributeClass != null &&
                a.AttributeClass.ContainingNamespace?.Name == "Binding" &&
                a.AttributeClass.ContainingNamespace?.ContainingNamespace?.Name == "Mond" &&
                a.AttributeClass.Name == name);
            return attribute != null;
        }

        public static T GetArgument<T>(this AttributeData attribute, int i = 0)
        {
            var args = attribute.ConstructorArguments;
            if (i >= args.Length)
            {
                return default;
            }

            return (T)args[i].Value;
        }

        public static bool HasDefaultConstructor(this INamedTypeSymbol klass)
        {
            return !klass.IsStatic && (klass.InstanceConstructors.Length == 0 || klass.InstanceConstructors.Any(c => c.Parameters.Length == 0));
        }

        public static List<INamedTypeSymbol> GetParentTypes(this ITypeSymbol type)
        {
            var result = new List<INamedTypeSymbol>();

            var parent = type.ContainingType;
            while (parent != null)
            {
                result.Add(parent);
                parent = parent.ContainingType;
            }

            return result;
        }

        public static string GetFullyQualifiedName(this ITypeSymbol symbol)
        {
            string result = null;

            var parent = symbol.ContainingType;
            while (parent != null)
            {
                result = result != null
                    ? parent.Name + "." + result
                    : parent.Name;

                parent = parent.ContainingType;
            }

            var ns = GetFullNamespace(symbol);
            var withNs = result != null
                ? ns + "." + result
                : ns;

            return string.IsNullOrEmpty(withNs)
                ? symbol.Name
                : withNs + "." + symbol.Name;
        }

        public static string GetFullNamespace(this ITypeSymbol symbol)
        {
            string result = null;

            var ns = symbol.ContainingNamespace;
            while (ns is { IsGlobalNamespace: false })
            {
                result = result != null
                    ? ns.Name + "." + result
                    : ns.Name;

                ns = ns.ContainingNamespace;
            }

            return result;
        }

        public static string ToCamelCase(this string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return identifier;
            }

            if (!char.IsLetter(identifier[0]))
            {
                return identifier;
            }

            var chars = identifier.ToCharArray();
            chars[0] = char.ToLowerInvariant(chars[0]);
            return new string(chars);
        }
    }
}
