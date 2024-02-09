using Microsoft.CodeAnalysis;

namespace Mond.SourceGenerator;

public partial class MondSourceGenerator
{
    private static void ModuleBindings(GeneratorExecutionContext context, INamedTypeSymbol module, IndentTextWriter writer)
    {
        var moduleName = module.GetAttributes().TryGetAttribute("MondModuleAttribute", out var moduleAttr)
            ? moduleAttr.GetArgument() ?? module.Name
            : module.Name;

        var qualifier = $"global::{module.GetFullNamespace()}.{module.Name}";
        var properties = GetProperties(context, module, true);
        var methods = GetMethods(context, module, true);
        var methodTables = MethodTable.Build(methods);

        writer.WriteLine("public sealed partial class Library : IMondLibrary");
        writer.OpenBracket();

        if (!module.IsStatic)
        {
            writer.WriteLine($"private readonly {qualifier} _instance;");
            writer.WriteLine();
            writer.WriteLine($"public Library({qualifier} instance = null)");
            writer.OpenBracket();
            writer.WriteLine($"_instance = instance ?? new {qualifier}();");
            writer.CloseBracket();
            writer.WriteLine();
        }

        writer.WriteLine("public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions(MondState state)");
        writer.OpenBracket();

        var bindsCtorArgs = module.IsStatic ? "" : "_instance";
        writer.WriteLine($"var binds = new Binds({bindsCtorArgs});");
        writer.WriteLine();

        if (moduleAttr != null && bool.TryParse(moduleAttr.GetArgument(1), out var bareMethods) && bareMethods)
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
                writer.WriteLine($"yield return new KeyValuePair<string, MondValue>(\"{table.Name}\", MondValue.Function(binds.{table.Name}__Dispatch));");
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
                writer.WriteLine($"result[\"{table.Name}\"] = MondValue.Function(binds.{table.Name}__Dispatch);");
            }

            writer.WriteLine();
            writer.WriteLine("result.Lock();");
            writer.WriteLine($"yield return new KeyValuePair<string, MondValue>(\"{moduleName}\", result);");
        }

        writer.CloseBracket();
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
                writer.WriteLine($"public MondValue {name}__Getter(MondState state, params MondValue[] args)");
                writer.OpenBracket();
                
                writer.WriteLine("if (args.Length != 0)");
                writer.OpenBracket();
                writer.WriteLine($"throw new MondRuntimeException(\"{moduleName}.get{name}: expected 0 arguments\");");
                writer.CloseBracket();

                writer.WriteLine($"var value = {propertyQualifier}.{property.Name};");
                writer.WriteLine($"return {ConvertToMondValue("value", property.Type)};");
                writer.CloseBracket();
                writer.WriteLine();
            }

            if (property.SetMethod is { DeclaredAccessibility: Accessibility.Public })
            {
                var parameter = new Parameter(property.SetMethod.Parameters[0]);

                writer.WriteLine($"public MondValue {name}__Setter(MondState state, params MondValue[] args)");
                writer.OpenBracket();

                writer.WriteLine($"if (args.Length != 1 || !{CompareArgument(0, parameter.MondTypes)})");
                writer.OpenBracket();
                writer.WriteLine($"throw new MondRuntimeException(\"{moduleName}.set{name}: expected 1 argument of type {parameter.TypeName}\");");
                writer.CloseBracket();

                writer.WriteLine($"{propertyQualifier}.{property.Name} = {ConvertFromMondValue("args[0]", property.Type)};");

                writer.WriteLine("return MondValue.Undefined;");
                writer.CloseBracket();
                writer.WriteLine();
            }
        }

        foreach (var table in methodTables)
        {
            writer.WriteLine($"public MondValue {table.Name}__Dispatch(MondState state, params MondValue[] args)");
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
                    writer.WriteLine($"if ({CompareArguments(method)})");
                    writer.OpenBracket();
                    CallMethod(writer, methodQualifier, method);
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
                CallMethod(writer, qualifier, method);
                writer.CloseBracket();
            }

            var errorMessage = GetMethodNotMatchedErrorMessage($"{moduleName}.{table.Name}: ", table);
            writer.WriteLine($"throw new MondRuntimeException(\"{EscapeForStringLiteral(errorMessage)}\");");

            writer.CloseBracket();
            writer.WriteLine();
        }

        writer.CloseBracket();
        writer.CloseBracket();
    }
}
