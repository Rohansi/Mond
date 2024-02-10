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

            context.AddSource($"{FullName(prototype)}.Prototype.g.cs", GenerateWith(context, prototype, PrototypeBindings));
        }

        foreach (var module in syntaxReceiver.Modules)
        {
            if (module.Arity != 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.CannotBindGeneric, module.Locations.First()));
                continue;
            }

            context.AddSource($"{FullName(module)}.Module.g.cs", GenerateWith(context, module, ModuleBindings));
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

            context.AddSource($"{FullName(klass)}.Class.g.cs", GenerateWith(context, klass, ClassBindings));
        }

        static string FullName(INamedTypeSymbol type)
        {
            return type.GetFullyQualifiedName();
        }
    }

    private static void CallMethod(IndentTextWriter writer, string qualifier, Method method, int argCount = 10000)
    {
        var isConstructor = method.Info.MethodKind == MethodKind.Constructor;
        var returnType = isConstructor
            ? method.Info.ContainingType
            : method.Info.ReturnType;
        var hasReturn = !SymbolEqualityComparer.Default.Equals(returnType, TypeLookup.Void);
        if (hasReturn)
        {
            writer.Write("var result = ");
        }
        writer.WriteLine(isConstructor
            ? $"new {method.Info.ContainingType.GetFullyQualifiedName()}({BindArguments(method, argCount)});"
            : $"{qualifier}.{method.Info.Name}({BindArguments(method, argCount)});");
        writer.WriteLine(hasReturn
            ? $"return {ConvertToMondValue("result", returnType)};"
            : "return MondValue.Undefined;");
    }

    private static string BindArguments(Method method, int argCount)
    {
        var valueIdx = 0;
        var args = new List<string>();
        foreach (var param in method.Parameters)
        {
            if (valueIdx >= argCount && param.Type == ParameterType.Value)
            {
                continue;
            }

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
            ParameterType.Value => ConvertFromMondValue(i, parameter.Info.Type),
            ParameterType.Params => $"args[{i}..]",
            ParameterType.State => "state",
            ParameterType.Instance => "instance",
            _ => throw new NotSupportedException($"{nameof(BindArgument)} {nameof(ParameterType)} {parameter.Type}"),
        };
    }

    private static string ConvertFromMondValue(int i, ITypeSymbol type)
    {
        var input = $"args[{i}]";
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

                if (type.TryGetAttribute("MondClassAttribute", out var attr))
                {
                    var name = attr.GetArgument<string>() ?? type.Name;
                    return $"({input}.UserData as global::{type.GetFullyQualifiedName()} ?? throw new MondRuntimeException(\"Unable to convert argument {i} to {name}\"))"; 
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

                if (type.HasAttribute("MondClassAttribute"))
                {
                    return $"MondValue.ClassInstance(state, {input}, \"{type.GetFullyQualifiedName()}\")";
                }

                return $"TODO({input})";
        }
    }

    private static string CompareArguments(Method method, int limit = 10000)
    {
        var argComparers = method.Parameters
            .Take(limit)
            .Where(p => p.Type == ParameterType.Value)
            .Select((p, i) => CompareArgument(i, p))
            .ToList();
        return argComparers.Count > 0
            ? string.Join(" && ", argComparers)
            : "true /* no arguments */";
    }

    private static string CompareArgument(int i, Parameter p)
    {
        var comparer = p.MondTypes.Length == 1 && p.MondTypes[0] == MondValueType.Undefined // special value for any
            ? $"(true /* args[{i}] is any */)"
            : "(" + string.Join(" || ", p.MondTypes.Select(t => $"args[{i}].Type == MondValueType.{t}")) + ")";

        return p.IsOptional
            ? $"(args.Length > {i} && {comparer})"
            : comparer;
    }

    private static string GetMethodNotMatchedErrorMessage(string prefix, MethodTable methodTable)
    {
        var sb = new StringBuilder();

        sb.Append(prefix);
        sb.AppendLine("argument types do not match any available functions");

        var methods = methodTable.Methods
            .SelectMany(l => l)
            .Concat(methodTable.ParamsMethods)
            .Distinct();

        foreach (var method in methods)
        {
            sb.Append("- ");
            sb.AppendLine(method.ToString());
        }

        return sb.ToString().Trim();
    }

    private static string EscapeForStringLiteral(string str)
    {
        return str
            .Replace("\r", "")
            .Replace(@"\", @"\\")
            .Replace("\n", @"\n");
    }

    private static List<(IMethodSymbol Method, string Name, string Identifier)> GetMethods(GeneratorExecutionContext context, INamedTypeSymbol klass, bool? isStatic = null)
    {
        var result = new List<(IMethodSymbol, string, string)>();
        foreach (var member in klass.GetMembers())
        {
            if (member is not IMethodSymbol { MethodKind: MethodKind.Ordinary } method || (isStatic != null && method.IsStatic != isStatic))
            {
                continue;
            }

            var attributes = method.GetAttributes();
            var hasFuncAttr = attributes.TryGetAttribute("MondFunctionAttribute", out var funcAttr);
            var hasOpAttr = attributes.TryGetAttribute("MondOperatorAttribute", out var opAttr);

            if (!hasFuncAttr && !hasOpAttr)
            {
                continue;
            }

            if (hasFuncAttr && hasOpAttr)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundMethodsCannotBeFunctionAndOperator, method.Locations.First()));
                continue;
            }

            if (method.DeclaredAccessibility != Accessibility.Public)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundMembersMustBePublic, method.Locations.First()));
                continue;
            }

            var name = hasFuncAttr
                ? (funcAttr.GetArgument<string>() ?? method.Name).ToCamelCase()
                : opAttr.GetArgument<string>();
            var ident = hasFuncAttr
                ? name
                : MondUtil.GetOperatorIdentifier(name);
            result.Add((method, name, ident));
        }

        return result;
    }

    private static List<(IPropertySymbol Property, string Name)> GetProperties(GeneratorExecutionContext context, INamedTypeSymbol klass, bool? isStatic = null)
    {
        var result = new List<(IPropertySymbol, string)>();
        foreach (var member in klass.GetMembers())
        {
            if (member is not IPropertySymbol property || (isStatic != null && property.IsStatic != isStatic))
            {
                continue;
            }

            if (!property.TryGetAttribute("MondFunctionAttribute", out var attr))
            {
                continue;
            }

            if (property.DeclaredAccessibility != Accessibility.Public)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundMembersMustBePublic, property.Locations.First()));
                continue;
            }

            result.Add((property, attr.GetArgument<string>() ?? property.Name));
        }

        return result;
    }

    private static List<IMethodSymbol> GetConstructors(GeneratorExecutionContext context, INamedTypeSymbol klass)
    {
        var result = new List<IMethodSymbol>();
        foreach (var member in klass.GetMembers())
        {
            if (member is not IMethodSymbol { MethodKind: MethodKind.Constructor } method)
            {
                continue;
            }

            if (!method.HasAttribute("MondConstructorAttribute"))
            {
                continue;
            }

            if (method.DeclaredAccessibility != Accessibility.Public)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundMembersMustBePublic, method.Locations.First()));
                continue;
            }

            result.Add(method);
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

        var parents = symbol.GetParentTypes();
        for (var i = parents.Count - 1; i >= 0; i--)
        {
            writer.WriteLine($"partial class {parents[i].Name}");
            writer.OpenBracket();
        }

        writer.WriteLine($"partial class {symbol.Name}");
        writer.OpenBracket();

        generator(context, symbol, writer);

        writer.CloseBracket();

        for (var i = 0; i < parents.Count; i++)
        {
            writer.CloseBracket();
        }

        if (ns != null)
        {
            writer.CloseBracket();
        }

        return stringBuilder.ToString();
    }
}
