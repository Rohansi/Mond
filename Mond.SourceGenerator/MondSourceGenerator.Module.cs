using Microsoft.CodeAnalysis;

namespace Mond.SourceGenerator;

public partial class MondSourceGenerator
{
    private static void ModuleBindings(GeneratorExecutionContext context, INamedTypeSymbol module, IndentTextWriter writer)
    {
        var moduleName = module.GetAttributes().TryGetAttribute("MondModuleAttribute", out var moduleAttr)
            ? moduleAttr.GetArgument<string>() ?? module.Name
            : module.Name;

        var useBareMethods = moduleAttr != null && moduleAttr.GetArgument<bool>(1);
        var qualifier = $"global::{module.GetFullyQualifiedName()}";
        var properties = GetProperties(context, module);
        var methods = GetMethods(context, module);
        var methodTables = MethodTable.Build(context, methods);

        writer.WriteLine("public sealed partial class Library : IMondLibrary");
        writer.OpenBracket();

        if (!module.IsStatic)
        {
            var canDefaultConstruct = module.HasDefaultConstructor();

            writer.WriteLine($"private readonly {qualifier} _instance;");
            writer.WriteLine();
            writer.WriteLine(canDefaultConstruct
                ? $"public Library({qualifier} instance = null)"
                : $"public Library({qualifier} instance)");
            writer.OpenBracket();
            writer.WriteLine(canDefaultConstruct
                ? $"_instance = instance ?? new {qualifier}();"
                : $"_instance = instance ?? throw new ArgumentNullException(nameof(instance));");
            writer.CloseBracket();
            writer.WriteLine();
        }

        writer.WriteLine("public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions(MondState state)");
        writer.OpenBracket();

        var bindsCtorArgs = module.IsStatic ? "" : "_instance";
        writer.WriteLine($"var binds = new Binds({bindsCtorArgs});");
        writer.WriteLine();

        if (useBareMethods)
        {
            foreach (var (property, name) in properties)
            {
                if (property.GetMethod is { DeclaredAccessibility: Accessibility.Public })
                {
                    writer.WriteLine($"yield return new KeyValuePair<string, MondValue>(\"get{name}\", MondValue.Function(binds.{name}__Getter));");
                }

                if (property.SetMethod is { DeclaredAccessibility: Accessibility.Public })
                {
                    writer.WriteLine($"yield return new KeyValuePair<string, MondValue>(\"set{name}\", MondValue.Function(binds.{name}__Setter));");
                }
            }

            foreach (var table in methodTables)
            {
                writer.WriteLine($"yield return new KeyValuePair<string, MondValue>(\"{table.Identifier}\", MondValue.Function(binds.{table.Identifier}__Dispatch));");
            }

            writer.WriteLine("yield break;");
        }
        else
        {
            writer.WriteLine("var result = MondValue.Object(state);");
            writer.WriteLine();

            foreach (var (property, name) in properties)
            {
                if (property.GetMethod is { DeclaredAccessibility: Accessibility.Public })
                {
                    writer.WriteLine($"result[\"get{name}\"] = MondValue.Function(binds.{name}__Getter);");
                }

                if (property.SetMethod is { DeclaredAccessibility: Accessibility.Public })
                {
                    writer.WriteLine($"result[\"set{name}\"] = MondValue.Function(binds.{name}__Setter);");
                }
            }

            foreach (var table in methodTables)
            {
                writer.WriteLine($"result[\"{table.Identifier}\"] = MondValue.Function(binds.{table.Identifier}__Dispatch);");
            }

            writer.WriteLine();
            writer.WriteLine("ModifyObject(result);");
            writer.WriteLine();
            writer.WriteLine("result.Lock();");
            writer.WriteLine($"yield return new KeyValuePair<string, MondValue>(\"{moduleName}\", result);");
        }

        writer.CloseBracket();

        if (!useBareMethods)
        {
            writer.WriteLine();
            writer.WriteLine("partial void ModifyObject(MondValue obj);");
        }

        writer.WriteLine();

        writer.WriteLine("private sealed class Binds");
        writer.OpenBracket();
        
        if (!module.IsStatic)
        {
            writer.WriteLine($"private readonly {qualifier} _instance;");
            writer.WriteLine();
            writer.WriteLine($"public Binds({qualifier} instance)");
            writer.OpenBracket();
            writer.WriteLine("_instance = instance;");
            writer.CloseBracket();
            writer.WriteLine();
        }

        foreach (var (property, name) in properties)
        {
            var propertyQualifier = property.IsStatic ? qualifier : "_instance";
            if (property.GetMethod is { DeclaredAccessibility: Accessibility.Public })
            {
                writer.WriteLine($"public MondValue {name}__Getter(MondState state, params Span<MondValue> args)");
                writer.OpenBracket();
                
                writer.WriteLine("if (args.Length != 0)");
                writer.OpenBracket();
                writer.WriteLine($"throw new MondRuntimeException(\"{moduleName}.get{name}: expected 0 arguments\");");
                writer.CloseBracket();

                writer.WriteLine($"var value = {propertyQualifier}.{property.Name};");
                writer.WriteLine($"return {ConvertToMondValue(context, "value", property.Type, property)};");
                writer.CloseBracket();
                writer.WriteLine();
            }

            if (property.SetMethod is { DeclaredAccessibility: Accessibility.Public })
            {
                var parameter = Parameter.Create(context, property.SetMethod.Parameters[0]);

                writer.WriteLine($"public MondValue {name}__Setter(MondState state, params Span<MondValue> args)");
                writer.OpenBracket();

                writer.WriteLine($"if (args.Length != 1 || !{CompareArgument(0, parameter)})");
                writer.OpenBracket();
                writer.WriteLine($"throw new MondRuntimeException(\"{moduleName}.set{name}: expected 1 argument of type {parameter.TypeName}\");");
                writer.CloseBracket();

                writer.WriteLine($"{propertyQualifier}.{property.Name} = {ConvertFromMondValue(context, 0, property.Type, property)};");

                writer.WriteLine("return MondValue.Undefined;");
                writer.CloseBracket();
                writer.WriteLine();
            }
        }

        foreach (var table in methodTables)
        {
            writer.WriteLine($"public MondValue {table.Identifier}__Dispatch(MondState state, params Span<MondValue> args)");
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
                    var methodQualifier = method.Info.IsStatic ? qualifier : "_instance";
                    writer.WriteLine($"if ({CompareArguments(method, 0, i)})");
                    writer.OpenBracket();
                    CallMethod(context, writer, methodQualifier, method, 0, i);
                    writer.CloseBracket();
                }
                writer.WriteLine("break;");
                writer.CloseBracket();
            }

            writer.CloseBracket();

            foreach (var method in table.ParamsMethods)
            {
                var methodQualifier = method.Info.IsStatic ? qualifier : "_instance";
                writer.WriteLine($"if (args.Length >= {method.RequiredMondParameterCount} && {CompareArguments(method)})");
                writer.OpenBracket();
                CallMethod(context, writer, methodQualifier, method, 0);
                writer.CloseBracket();
            }

            writer.WriteLine();
            var errorPrefix = useBareMethods
                ? $"{table.Name}: "
                : $"{moduleName}.{table.Name}: ";
            var errorMessage = GetMethodNotMatchedErrorMessage(errorPrefix, table);
            writer.WriteLine($"throw new MondRuntimeException(\"{EscapeForStringLiteral(errorMessage)}\");");

            writer.CloseBracket();
            writer.WriteLine();
        }

        writer.CloseBracket();
        writer.CloseBracket();
    }
}
