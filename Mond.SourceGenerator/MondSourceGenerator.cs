using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Mond.SourceGenerator;

[Generator]
public partial class MondSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
#if DEBUG
        if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }
#endif

        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not SyntaxReceiver syntaxReceiver)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingSyntaxReceiver, Location.None));
            return;
        }

        if (!TypeLookup.Initialize(context))
        {
            return;
        }

        foreach (var location in syntaxReceiver.MissingPartials)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundClassesMustBePartial, location));
        }

        foreach (var prototype in syntaxReceiver.Prototypes)
        {
            if (prototype.Arity != 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.CannotBindGeneric, prototype.Locations.First()));
                continue;
            }

            context.AddSource($"{prototype.Name}.Prototype.g.cs", GenerateWith(context, prototype, PrototypeBindings));
        }

        foreach (var module in syntaxReceiver.Modules)
        {
            if (module.Arity != 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.CannotBindGeneric, module.Locations.First()));
                continue;
            }

            context.AddSource($"{module.Name}.Module.g.cs", GenerateWith(context, module, ModuleBindings));
        }

        foreach (var klass in syntaxReceiver.Classes)
        {
            if (klass.Arity != 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.CannotBindGeneric, klass.Locations.First()));
                continue;
            }

            if (klass.IsStatic)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.ClassesCannotBeStatic, klass.Locations.First()));
                continue;
            }

            context.AddSource($"{klass.Name}.Class.g.cs", GenerateWith(context, klass, ClassBindings));
        }
    }

    private static void CallMethod(IndentTextWriter writer, string qualifier, Method method)
    {
        var hasReturn = !method.Info.ReturnsVoid;
        if (hasReturn)
        {
            writer.Write("var result = ");
        }
        writer.WriteLine($"{qualifier}.{method.Info.Name}({BindArguments(method)});");
        writer.WriteLine(hasReturn
            ? $"return {ConvertToMondValue("result", method.Info.ReturnType)};"
            : "return MondValue.Undefined;");
    }

    private static string BindArguments(Method method)
    {
        var valueIdx = 0;
        var args = new List<string>();
        foreach (var param in method.Parameters)
        {
            args.Add(BindArgument(valueIdx, param));

            if (param.Type == ParameterType.Value)
            {
                valueIdx++;
            }
        }

        return string.Join(", ", args);
    }

    private static string BindArgument(int i, Parameter parameter)
    {
        return parameter.Type switch
        {
            ParameterType.Value => ConvertFromMondValue($"args[{i}]", parameter.Info.Type),
            ParameterType.Params => $"args[{i}..]",
            ParameterType.State => "state",
            ParameterType.Instance => "instance",
            _ => throw new NotSupportedException($"{nameof(BindArgument)} {nameof(ParameterType)} {parameter.Type}"),
        };
    }

    private static string ConvertFromMondValue(string input, ITypeSymbol type)
    {
        switch (type.SpecialType)
        {
            case SpecialType.System_Double:
                return $"(double){input}";
            case SpecialType.System_Single:
                return $"(float){input}";
            case SpecialType.System_Int32:
                return $"(int){input}";
            case SpecialType.System_UInt32:
                return $"(uint){input}";
            case SpecialType.System_Int16:
                return $"(short){input}";
            case SpecialType.System_UInt16:
                return $"(ushort){input}";
            case SpecialType.System_SByte:
                return $"(sbyte){input}";
            case SpecialType.System_Byte:
                return $"(byte){input}";
            case SpecialType.System_String:
                return $"(string){input}";
            case SpecialType.System_Boolean:
                return $"(bool){input}";
            default:
                if (SymbolEqualityComparer.Default.Equals(type, TypeLookup.MondValue))
                {
                    return input;
                }

                if (SymbolEqualityComparer.Default.Equals(type, TypeLookup.MondValueNullable))
                {
                    return $"({input} == MondValue.Undefined ? null : (MondValue?){input})";
                }

                return $"TODO({input})";
        }
    }

    private static string ConvertToMondValue(string input, ITypeSymbol type)
    {
        switch (type.SpecialType)
        {

            case SpecialType.System_Double:
            case SpecialType.System_Single:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_SByte:
            case SpecialType.System_Byte:
            case SpecialType.System_String:
            case SpecialType.System_Boolean:
                return $"(MondValue){input}";

            default:
                if (SymbolEqualityComparer.Default.Equals(type, TypeLookup.MondValue))
                {
                    return input;
                }

                if (SymbolEqualityComparer.Default.Equals(type, TypeLookup.MondValueNullable))
                {
                    return $"({input} ?? MondValue.Undefined)";
                }

                return $"TODO({input})";
        }
    }

    private static string CompareArguments(Method method)
    {
        var argComparers = method.Parameters
            .Where(p => p.Type == ParameterType.Value)
            .Select((p, i) => CompareArgument(i, p.MondTypes))
            .ToList();
        return argComparers.Count > 0
            ? string.Join(" && ", argComparers)
            : "true /* no arguments */";
    }

    private static string CompareArgument(int i, MondValueType[] types)
    {
        if (types.Length == 1 && types[0] == MondValueType.Undefined)
        {
            // special value for any
            return $"(true /* arg[{i}] is any */)";
        }

        return "(" + string.Join(" || ", types.Select(t => $"args[{i}].Type == MondValueType.{t}")) + ")";
    }

    private static List<(IMethodSymbol Method, string Name)> GetMethods(GeneratorExecutionContext context, INamedTypeSymbol klass, bool isStatic)
    {
        var result = new List<(IMethodSymbol, string)>();
        foreach (var member in klass.GetMembers())
        {
            if (member is not IMethodSymbol { MethodKind: MethodKind.Ordinary } method || method.IsStatic != isStatic)
            {
                continue;
            }

            var attributes = method.GetAttributes();
            if (!attributes.TryGetAttribute("MondFunctionAttribute", out var attr))
            {
                continue;
            }

            if (method.DeclaredAccessibility != Accessibility.Public)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundMembersMustBePublic, method.Locations.First()));
                continue;
            }

            result.Add((method, attr.GetArgument() ?? method.Name));
        }

        return result;
    }

    private static List<(IPropertySymbol Property, string Name)> GetProperties(GeneratorExecutionContext context, INamedTypeSymbol klass, bool isStatic)
    {
        var result = new List<(IPropertySymbol, string)>();
        foreach (var member in klass.GetMembers())
        {
            if (member is not IPropertySymbol property || property.IsStatic != isStatic)
            {
                continue;
            }

            var attributes = property.GetAttributes();
            if (!attributes.TryGetAttribute("MondFunctionAttribute", out var attr))
            {
                continue;
            }

            if (property.DeclaredAccessibility != Accessibility.Public)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundMembersMustBePublic, property.Locations.First()));
                continue;
            }

            result.Add((property, attr.GetArgument() ?? property.Name));
        }

        return result;
    }

    private delegate void GeneratorAction(GeneratorExecutionContext context, INamedTypeSymbol symbol, IndentTextWriter writer);

    private static string GenerateWith(GeneratorExecutionContext context, INamedTypeSymbol symbol, GeneratorAction generator)
    {
        var stringBuilder = new StringBuilder();
        using var stringWriter = new StringWriter(stringBuilder);
        using var writer = new IndentTextWriter(stringWriter);

        writer.WriteLine("// <auto-generated />");
        writer.WriteLine();
        writer.WriteLine("#pragma warning disable CS0162 // Unreachable code detected");
        writer.WriteLine();
        writer.WriteLine("using System;");
        writer.WriteLine("using System.Collections.Generic;");
        writer.WriteLine("using Mond;");
        writer.WriteLine("using Mond.Libraries;");
        writer.WriteLine();

        var ns = symbol.GetFullNamespace();
        if (ns != null)
        {
            writer.WriteLine($"namespace {ns}");
            writer.OpenBracket();
        }

        writer.WriteLine($"partial class {symbol.Name}");
        writer.OpenBracket();

        generator(context, symbol, writer);

        writer.CloseBracket();

        if (ns != null)
        {
            writer.CloseBracket();
        }

        return stringBuilder.ToString();
    }
}
