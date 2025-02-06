using Microsoft.CodeAnalysis;

namespace Mond.SourceGenerator;

public partial class MondSourceGenerator
{
    private static void PrototypeBindings(GeneratorExecutionContext context, INamedTypeSymbol prototype, IndentTextWriter writer)
    {
        var prototypeName = prototype.GetAttributes().TryGetAttribute("MondPrototypeAttribute", out var prototypeAttr)
            ? prototypeAttr.GetArgument<string>() ?? prototype.Name
            : prototype.Name;

        var methods = GetMethods(context, prototype, true);
        var methodTables = MethodTable.Build(context, methods);

        writer.WriteLine("private sealed class PrototypeObject");
        writer.OpenBracket();

        writer.WriteLine("public static MondValue Build(MondValue? basePrototype = null)");
        writer.OpenBracket();
        writer.WriteLine("var result = MondValue.Object();");
        writer.WriteLine("var dict = result.ObjectValue.Values;");
        writer.WriteLine();

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

        foreach (var table in methodTables)
        {
            writer.WriteLine($"private static MondValue {table.Identifier}__Dispatch(MondState state, params MondValue[] args)");
            writer.OpenBracket();

            writer.WriteLine("if (args.Length < 1)");
            writer.OpenBracket();
            writer.WriteLine($"throw new MondRuntimeException(\"{prototypeName}.{table.Name}: missing instance argument\");");
            writer.CloseBracket();
            writer.WriteLine();

            writer.WriteLine("var instance = args[0];");
            writer.WriteLine("switch (args.Length - 1)");
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
                    writer.WriteLine($"if ({CompareArguments(method, 1, i)})");
                    writer.OpenBracket();
                    CallMethod(context, writer, qualifier, method, 1, i);
                    writer.CloseBracket();
                }
                writer.WriteLine("break;");
                writer.CloseBracket();
            }

            writer.CloseBracket();

            foreach (var method in table.ParamsMethods) 
            {
                writer.WriteLine($"if (args.Length >= {1 + method.RequiredMondParameterCount} && {CompareArguments(method, 1)})");
                writer.OpenBracket();
                CallMethod(context, writer, qualifier, method, 1);
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
