using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Mond.SourceGenerator;

public partial class MondSourceGenerator
{
    /// <summary>
    /// Set <c>MondDefinitionsFile</c> in the project to write a JSON description of everything the
    /// bindings expose. The Mond VS Code extension reads these files for completion and hover, so
    /// anyone embedding a customized Mond can ship metadata for their own modules the same way.
    /// </summary>
    private const string DefinitionsFileOption = "build_property.MondDefinitionsFile";

    /// <summary>Bumped when the shape of the file changes in a way consumers must notice.</summary>
    private const int DefinitionsVersion = 1;

    private static void EmitDefinitions(
        SourceProductionContext context,
        AnalyzerConfigOptionsProvider options,
        TypeLookup types,
        IEnumerable<INamedTypeSymbol> modules,
        IEnumerable<INamedTypeSymbol> classes,
        IEnumerable<INamedTypeSymbol> prototypes)
    {
        if (!options.GlobalOptions.TryGetValue(DefinitionsFileOption, out var outputPath) ||
            string.IsNullOrWhiteSpace(outputPath))
        {
            return;
        }

        var json = new JsonWriter();
        json.OpenObject();
        json.Property("version", DefinitionsVersion);

        json.Name("globals");
        json.OpenArray();
        foreach (var module in modules.OrderBy(GetBindingName, StringComparer.Ordinal))
        {
            WriteModule(json, context, types, module);
        }
        foreach (var klass in classes.OrderBy(GetBindingName, StringComparer.Ordinal))
        {
            WriteClass(json, context, types, klass);
        }
        json.CloseArray();

        json.Name("prototypes");
        json.OpenArray();
        foreach (var prototype in prototypes.OrderBy(GetBindingName, StringComparer.Ordinal))
        {
            WritePrototype(json, context, types, prototype);
        }
        json.CloseArray();

        json.CloseObject();

        WriteIfChanged(outputPath.Trim(), json.ToString());
    }

    private static void WriteModule(JsonWriter json, SourceProductionContext context, TypeLookup types, INamedTypeSymbol module)
    {
        var moduleAttr = module.GetAttribute("MondModuleAttribute");
        var name = moduleAttr?.GetArgument<string>() ?? module.Name;
        var bareMethods = moduleAttr != null && moduleAttr.GetArgument<bool>(1);
        var members = GetMembers(context, types, module, null, true);

        if (bareMethods)
        {
            // a bare module contributes its members straight to the global scope
            foreach (var member in members)
            {
                WriteMember(json, member, "function");
            }

            return;
        }

        json.OpenObject();
        json.Property("name", name);
        json.Property("kind", "module");
        WriteDocumentation(json, module);
        WriteMembers(json, members);
        json.CloseObject();
    }

    private static void WriteClass(JsonWriter json, SourceProductionContext context, TypeLookup types, INamedTypeSymbol klass)
    {
        var name = klass.GetAttribute("MondClassAttribute")?.GetArgument<string>() ?? klass.Name;

        var constructors = GetConstructors(context, klass);
        if (constructors.Count == 0)
        {
            return; // cannot be constructed from a script, so there is nothing to suggest
        }

        var constructorMethods = MethodTable
            .Build(context, types, constructors.Select(c => (c, name, name)))
            .SelectMany(t => t.AllMethods());

        json.OpenObject();
        json.Property("name", name);
        json.Property("kind", "class");
        WriteSignatures(json, types, constructorMethods);
        WriteDocumentation(json, klass);
        WriteMembers(json, GetMembers(context, types, klass, false, true));
        json.CloseObject();
    }

    private static void WritePrototype(JsonWriter json, SourceProductionContext context, TypeLookup types, INamedTypeSymbol prototype)
    {
        var name = prototype.GetAttribute("MondPrototypeAttribute")?.GetArgument<string>() ?? prototype.Name;

        json.OpenObject();
        json.Property("name", name);
        WriteDocumentation(json, prototype);
        WriteMembers(json, GetMembers(context, types, prototype, true, false));
        json.CloseObject();
    }

    private static void WriteMembers(JsonWriter json, List<Member> members)
    {
        json.Name("members");
        json.OpenArray();
        foreach (var member in members)
        {
            WriteMember(json, member, "method");
        }
        json.CloseArray();
    }

    private static void WriteMember(JsonWriter json, Member member, string kind)
    {
        json.OpenObject();
        json.Property("name", member.Name);
        json.Property("kind", kind);

        if (member.Signatures.Count > 0)
        {
            json.Name("signatures");
            json.OpenArray();
            foreach (var signature in member.Signatures)
            {
                json.Value(signature);
            }
            json.CloseArray();
        }

        if (!string.IsNullOrEmpty(member.Documentation))
        {
            json.Property("documentation", member.Documentation);
        }

        json.CloseObject();
    }

    private sealed class Member
    {
        public string Name { get; set; }
        public List<string> Signatures { get; set; } = [];
        public string Documentation { get; set; }
    }

    private static List<Member> GetMembers(
        SourceProductionContext context,
        TypeLookup types,
        INamedTypeSymbol symbol,
        bool? isStatic,
        bool includeProperties)
    {
        var result = new List<Member>();

        if (includeProperties)
        {
            // bound properties are exposed as a pair of functions, not as a field
            foreach (var (property, name) in GetProperties(context, symbol, isStatic))
            {
                var documentation = GetSummary(property);
                var typeName = GetMondTypeName(types, property.Type) ?? "any";

                if (property.GetMethod is { DeclaredAccessibility: Accessibility.Public })
                {
                    result.Add(new Member
                    {
                        Name = $"get{name}",
                        Signatures = [$"get{name}(): {typeName}"],
                        Documentation = documentation,
                    });
                }

                if (property.SetMethod is { DeclaredAccessibility: Accessibility.Public })
                {
                    result.Add(new Member
                    {
                        Name = $"set{name}",
                        Signatures = [$"set{name}(value: {typeName})"],
                        Documentation = documentation,
                    });
                }
            }
        }

        foreach (var table in MethodTable.Build(context, types, GetMethods(context, symbol, isStatic)))
        {
            // the compiler emits calls to these to implement operators, nobody writes them by hand
            if (table.Name.StartsWith("op_", StringComparison.Ordinal))
            {
                continue;
            }

            var overloads = table.AllMethods().ToList();
            result.Add(new Member
            {
                Name = table.Name,
                Signatures = overloads.Select(m => GetSignature(types, m)).Distinct(StringComparer.Ordinal).ToList(),
                Documentation = overloads
                    .Select(m => GetSummary(m.Info))
                    .FirstOrDefault(d => !string.IsNullOrEmpty(d)),
            });
        }

        return result.OrderBy(m => m.Name, StringComparer.Ordinal).ToList();
    }

    private static void WriteSignatures(JsonWriter json, TypeLookup types, IEnumerable<Method> methods)
    {
        var signatures = methods
            .Select(m => GetSignature(types, m))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (signatures.Count == 0)
        {
            return;
        }

        json.Name("signatures");
        json.OpenArray();
        foreach (var signature in signatures)
        {
            json.Value(signature);
        }
        json.CloseArray();
    }

    private static string GetSignature(TypeLookup types, Method method)
    {
        var sb = new StringBuilder();
        sb.Append(method.Name);
        sb.Append('(');

        var first = true;
        foreach (var parameter in method.Parameters)
        {
            // state and instance parameters are supplied by the runtime, scripts never pass them
            if (parameter.Type != ParameterType.Value && parameter.Type != ParameterType.Params)
            {
                continue;
            }

            if (!first)
            {
                sb.Append(", ");
            }

            first = false;

            if (parameter.Type == ParameterType.Params)
            {
                sb.Append("...");
            }

            sb.Append(parameter.Info.Name.ToCamelCase());

            if (parameter.IsOptional)
            {
                sb.Append('?');
            }

            if (parameter.Type == ParameterType.Value)
            {
                sb.Append(": ");
                sb.Append(parameter.TypeName);
            }
        }

        sb.Append(')');

        var returnType = method.Info.MethodKind == MethodKind.Constructor
            ? null
            : GetMondTypeName(types, method.Info.ReturnType);

        if (returnType != null)
        {
            sb.Append(": ");
            sb.Append(returnType);
        }

        return sb.ToString();
    }

    /// <summary>Best effort mapping of a bound CLR type onto the Mond type a script sees.</summary>
    private static string GetMondTypeName(TypeLookup types, ITypeSymbol type)
    {
        if (type == null || SymbolEqualityComparer.Default.Equals(type, types.Void))
        {
            return null;
        }

        if (types.TypeCheckMap.TryGetValue(type, out var mondTypes))
        {
            return mondTypes[0].GetName();
        }

        if (SymbolEqualityComparer.Default.Equals(type, types.Task))
        {
            return null;
        }

        if (type is INamedTypeSymbol { IsGenericType: true } named &&
            SymbolEqualityComparer.Default.Equals(named.ConstructedFrom, types.TaskOfT))
        {
            return GetMondTypeName(types, named.TypeArguments[0]);
        }

        return "any";
    }

    private static string GetBindingName(INamedTypeSymbol symbol)
    {
        return symbol.GetAttribute("MondModuleAttribute")?.GetArgument<string>()
            ?? symbol.GetAttribute("MondClassAttribute")?.GetArgument<string>()
            ?? symbol.GetAttribute("MondPrototypeAttribute")?.GetArgument<string>()
            ?? symbol.Name;
    }

    private static void WriteDocumentation(JsonWriter json, ISymbol symbol)
    {
        var summary = GetSummary(symbol);
        if (!string.IsNullOrEmpty(summary))
        {
            json.Property("documentation", summary);
        }
    }

    /// <summary>Pulls the <c>summary</c> out of an XML doc comment as a single line of plain text.</summary>
    private static string GetSummary(ISymbol symbol)
    {
        var xml = symbol.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(xml))
        {
            return null;
        }

        try
        {
            var summary = XDocument.Parse(xml).Root?.Element("summary");
            if (summary == null)
            {
                return null;
            }

            var text = string.Join(" ", summary.Value
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0));

            return string.IsNullOrEmpty(text) ? null : text;
        }
        catch (XmlException)
        {
            return null;
        }
    }

    /// <summary>
    /// Only touches the file when the content changed - the generator runs once per target framework,
    /// and rewriting it every build would keep invalidating anything watching the file.
    /// </summary>
    /// <remarks>
    /// RS1035 exists because generators also run inside the IDE, where writing files is a bad idea.
    /// This one is opt-in through an MSBuild property that only a real command line build sets, and
    /// the write is a no-op when nothing changed, so it never disturbs an editing session.
    /// </remarks>
#pragma warning disable RS1035
    private static void WriteIfChanged(string path, string content)
    {
        try
        {
            if (File.Exists(path) && File.ReadAllText(path) == content)
            {
                return;
            }

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, content);
        }
        catch (IOException)
        {
            // another target framework is writing the same content at the same time
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
#pragma warning restore RS1035
}
