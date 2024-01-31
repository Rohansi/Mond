using Microsoft.CodeAnalysis;

namespace Mond.SourceGenerator;

public partial class MondSourceGenerator
{
    private static void PrototypeBindings(GeneratorExecutionContext context, INamedTypeSymbol prototype, IndentTextWriter writer)
    {
        var prototypeName = prototype.GetAttributes().TryGetAttribute("MondPrototypeAttribute", out var prototypeAttr)
            ? prototypeAttr.GetArgument() ?? prototype.Name
            : prototype.Name;

        var properties = GetProperties(context, prototype, true);
        var methods = GetMethods(context, prototype, true);
        var methodTables = MethodTable.Build(methods);

        writer.WriteLine("private sealed class PrototypeObject");
        writer.OpenBracket();

        writer.WriteLine("public static MondValue Build()");
        writer.OpenBracket();
        writer.WriteLine("var result = MondValue.Object();");

        foreach (var (property, name) in properties)
        {
            if (property.GetMethod is { DeclaredAccessibility: Accessibility.Public })
            {
                writer.WriteLine($"result[\"get{name}\"] = MondValue.Function({name}__Getter);");
            }

            if (property.SetMethod is { DeclaredAccessibility: Accessibility.Public })
            {
                writer.WriteLine($"result[\"set{name}\"] = MondValue.Function({name}__Setter);");
            }
        }

        foreach (var table in methodTables)
        {
            writer.WriteLine($"result[\"{table.Name}\"] = MondValue.Function({table.Name}__Dispatch);");
        }

        writer.WriteLine("return result;");
        writer.CloseBracket();
        writer.WriteLine();

        var qualifier = $"global::{prototype.GetFullNamespace()}.{prototype.Name}";

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

                writer.WriteLine($"if (args.Length != 1 || !{CompareArgument(0, parameter.MondTypes)})");
                writer.OpenBracket();
                writer.WriteLine($"throw new MondRuntimeException(\"{prototypeName}.set{name}: expected 1 argument of type {parameter.TypeName}\");");
                writer.CloseBracket();

                writer.WriteLine($"{qualifier}.{property.Name} = {ConvertFromMondValue("args[0]", property.Type)};");

                writer.WriteLine("return default;");
                writer.CloseBracket();
                writer.WriteLine();
            }
        }

        foreach (var table in methodTables)
        {
            writer.WriteLine($"private static MondValue {table.Name}__Dispatch(MondState state, MondValue instance, params MondValue[] args)");
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
                    writer.WriteLine($"if ({CompareArguments(method)})");
                    writer.OpenBracket();
                    CallMethod(writer, qualifier, method);
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

            writer.WriteLine("return default;"); // todo: throw exception - no method matched

            writer.CloseBracket();
            writer.WriteLine();
        }

        writer.CloseBracket();
    }
}
