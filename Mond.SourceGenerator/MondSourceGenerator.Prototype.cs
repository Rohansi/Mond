using Microsoft.CodeAnalysis;

namespace Mond.SourceGenerator;

public partial class MondSourceGenerator
{
    private static void PrototypeBindings(GeneratorExecutionContext context, INamedTypeSymbol prototype, IndentTextWriter writer)
    {
        var prototypeName = prototype.GetAttributes().TryGetAttribute("MondPrototypeAttribute", out var prototypeAttr)
            ? prototypeAttr.GetArgument<string>() ?? prototype.Name
            : prototype.Name;

        var properties = GetProperties(context, prototype, true);
        var methods = GetMethods(context, prototype, true);
        var methodTables = MethodTable.Build(methods);

        writer.WriteLine("private sealed class PrototypeObject");
        writer.OpenBracket();

        writer.WriteLine("public static MondValue Build(MondValue? basePrototype = null)");
        writer.OpenBracket();
        writer.WriteLine("var result = MondValue.Object();");
        writer.WriteLine("var dict = result.ObjectValue.Values;");
        writer.WriteLine();

        foreach (var (property, name) in properties)
        {
            if (property.GetMethod is { DeclaredAccessibility: Accessibility.Public })
            {
                writer.WriteLine($"dict[\"get{name}\"] = MondValue.Function({name}__Getter);");
            }

            if (property.SetMethod is { DeclaredAccessibility: Accessibility.Public })
            {
                writer.WriteLine($"dict[\"set{name}\"] = MondValue.Function({name}__Setter);");
            }
        }

        foreach (var table in methodTables)
        {
            writer.WriteLine($"dict[\"{table.Identifier}\"] = MondValue.Function({table.Identifier}__Dispatch);");
        }

        writer.WriteLine();
        writer.WriteLine("if (basePrototype != null)");
        writer.OpenBracket();
        writer.WriteLine("result.ObjectValue.HasPrototype = true;");
        writer.WriteLine("result.ObjectValue.Prototype = basePrototype.Value;");
        writer.CloseBracket();
        writer.WriteLine();

        writer.WriteLine("result.Lock();");
        writer.WriteLine("return result;");
        writer.CloseBracket();
        writer.WriteLine();

        var qualifier = $"global::{prototype.GetFullyQualifiedName()}";

        foreach (var (property, name) in properties)
        {
            if (property.GetMethod is { DeclaredAccessibility: Accessibility.Public })
            {
                writer.WriteLine($"private static MondValue {name}__Getter(MondState state, MondValue instance, params MondValue[] args)");
                writer.OpenBracket();

                writer.WriteLine("if (args.Length != 0)");
                writer.OpenBracket();
                writer.WriteLine($"throw new MondRuntimeException(\"{prototypeName}.get{name}: expected 0 arguments\");");
                writer.CloseBracket();

                writer.WriteLine($"var value = {qualifier}.{property.Name};");
                writer.WriteLine($"return {ConvertToMondValue("value", property.Type)};");
                writer.CloseBracket();
                writer.WriteLine();
            }

            if (property.SetMethod is { DeclaredAccessibility: Accessibility.Public })
            {
                var parameter = new Parameter(property.SetMethod.Parameters[0]);

                writer.WriteLine($"private static MondValue {name}__Setter(MondState state, MondValue instance, params MondValue[] args)");
                writer.OpenBracket();

                writer.WriteLine($"if (args.Length != 1 || !{CompareArgument(0, parameter)})");
                writer.OpenBracket();
                writer.WriteLine($"throw new MondRuntimeException(\"{prototypeName}.set{name}: expected 1 argument of type {parameter.TypeName}\");");
                writer.CloseBracket();

                writer.WriteLine($"{qualifier}.{property.Name} = {ConvertFromMondValue(0, property.Type)};");

                writer.WriteLine("return MondValue.Undefined;");
                writer.CloseBracket();
                writer.WriteLine();
            }
        }

        foreach (var table in methodTables)
        {
            writer.WriteLine($"private static MondValue {table.Identifier}__Dispatch(MondState state, MondValue instance, params MondValue[] args)");
            writer.OpenBracket();

            writer.WriteLine("switch (args.Length)");
            writer.OpenBracket();

            for (var i = 0; i < table.Methods.Count; i++)
            {
                var tableMethods = table.Methods[i];
                if (tableMethods.Count == 0)
                {
                    continue;
                }

                writer.WriteLine($"case {i}:");
                writer.OpenBracket();
                foreach (var method in tableMethods)
                {
                    writer.WriteLine($"if ({CompareArguments(method, i)})");
                    writer.OpenBracket();
                    CallMethod(writer, qualifier, method, i);
                    writer.CloseBracket();
                }
                writer.WriteLine("break;");
                writer.CloseBracket();
            }

            writer.CloseBracket();

            foreach (var method in table.ParamsMethods) 
            {
                writer.WriteLine($"if (args.Length >= {method.RequiredMondParameterCount} && {CompareArguments(method)})");
                writer.OpenBracket();
                CallMethod(writer, qualifier, method);
                writer.CloseBracket();
            }

            writer.WriteLine();
            var errorMessage = GetMethodNotMatchedErrorMessage($"{prototypeName}.{table.Name}: ", table);
            writer.WriteLine($"throw new MondRuntimeException(\"{EscapeForStringLiteral(errorMessage)}\");");

            writer.CloseBracket();
            writer.WriteLine();
        }

        writer.CloseBracket();
    }
}
