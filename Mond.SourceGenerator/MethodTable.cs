using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Mond.SourceGenerator;

internal partial class MethodTable
{
    public readonly string Name;
    public readonly string Identifier;
    public readonly List<List<Method>> Methods;
    public readonly List<Method> ParamsMethods;

    public MethodTable(string name, string identifier, List<List<Method>> methods, List<Method> paramsMethods)
    {
        Name = name;
        Identifier = identifier;
        Methods = methods;
        ParamsMethods = paramsMethods;
    }
}

internal class Method : IComparable<Method>
{
    public readonly IMethodSymbol Info;

    public readonly string Name;
    public readonly string Identifier;

    public readonly int MondParameterCount;         // maximum number of ParameterType.Value parameters
    public readonly int RequiredMondParameterCount; // number of required ParameterType.Value parameters

    public readonly List<Parameter> Parameters;
    public readonly List<Parameter> ValueParameters;

    public readonly bool HasParams;

    public Method(GeneratorExecutionContext context, string name, string identifier, IMethodSymbol info)
    {
        Name = name;
        Identifier = identifier;
        Info = info;

        var parameters = info.Parameters;

        Parameters = parameters
            .Select(p => Parameter.Create(context, p))
            .ToList();

        ValueParameters = Parameters
            .Where(p => p.Type == ParameterType.Value)
            .ToList();

        MondParameterCount = ValueParameters.Count;
        RequiredMondParameterCount = ValueParameters.Count(p => !p.IsOptional);

        HasParams = Parameters.Any(p => p.Type == ParameterType.Params);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(Name);
        sb.Append('(');

        string sep = null;

        foreach (var p in Parameters.Where(p => p.Type == ParameterType.Value || p.Type == ParameterType.Params))
        {
            if (sep != null)
                sb.Append(sep);

            sb.Append(p);

            if (p.IsOptional)
                sb.Append('?');

            sep = ", ";
        }

        sb.Append(')');
        return sb.ToString();
    }

    public int CompareTo(Method other)
    {
        var x = this;
        var y = other;

        var xParams = x.Parameters.Where(p => p.Type == ParameterType.Value || p.Type == ParameterType.Params).ToList();
        var yParams = y.Parameters.Where(p => p.Type == ParameterType.Value || p.Type == ParameterType.Params).ToList();

        for (var i = 0; ; i++)
        {
            if (i >= xParams.Count && i >= yParams.Count)
                return 0; // x == y

            if (i >= xParams.Count)
                return -1; // x < y

            if (i >= yParams.Count)
                return 1; // x > y

            var diff = xParams[i].Priority - yParams[i].Priority;
            if (diff != 0)
                return diff;
        }
    }
}

internal enum ParameterType
{
    Unsupported,
    Value,
    Params,
    State,
    Instance,
}

internal class Parameter
{
    private static readonly MondValueType[] AnyTypes = [MondValueType.Undefined];
    private static readonly MondValueType[] ObjectTypes = [MondValueType.Object];

    public readonly IParameterSymbol Info;
    public readonly bool IsOptional;

    public ParameterType Type { get; private set; }
    public string TypeName { get; private set; }

    public int Priority { get; private set; }

    public MondValueType[] MondTypes { get; private set; }

    public ITypeSymbol UserDataType { get; private set; }

    private Parameter(IParameterSymbol info)
    {
        Info = info;
        IsOptional = info.IsOptional;
    }

    public override string ToString()
    {
        return TypeName;
    }

    public static Parameter Create(GeneratorExecutionContext context, IParameterSymbol info)
    {
        var param = new Parameter(info);
        var paramType = info.Type;

        if (TypeLookup.TypeCheckMap.TryGetValue(paramType, out var mondTypes))
        {
            param.Type = ParameterType.Value;
            param.TypeName = mondTypes[0].GetName();

            if (SymbolEqualityComparer.Default.Equals(paramType, TypeLookup.Bool))
            {
                param.Priority = 10;
            }
            else if (TypeLookup.NumberTypes.Contains(paramType))
            {
                param.Priority = 20;
            }
            else if (SymbolEqualityComparer.Default.Equals(paramType, TypeLookup.String))
            {
                param.Priority = 30;
            }

            param.MondTypes = mondTypes;
        }
        else if (SymbolEqualityComparer.Default.Equals(paramType, TypeLookup.MondValue))
        {
            if (info.HasAttribute("MondInstanceAttribute"))
            {
                param.Type = ParameterType.Instance;
                param.TypeName = "instance";
            }
            else
            {
                param.Type = ParameterType.Value;
                param.TypeName = "any";
                param.Priority = 100;
                param.MondTypes = AnyTypes;
            }
        }
        else if (SymbolEqualityComparer.Default.Equals(paramType, TypeLookup.MondValueNullable))
        {
            param.Type = ParameterType.Value;
            param.TypeName = "any?";
            param.Priority = 100;
            param.MondTypes = AnyTypes;
        }
        else if (info.IsParams)
        {
            if (TypeLookup.MondValueSpan == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.SpanTypeNotFound, info.Locations.First()));
            }
            else if (!SymbolEqualityComparer.Default.Equals(paramType, TypeLookup.MondValueSpan))
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundMethodParamsMustBeSpan, info.Locations.First(), info.Type.GetFullyQualifiedName()));
            }

            param.Type = ParameterType.Params;
            param.TypeName = "...";
            param.Priority = 75;
        }
        else if (SymbolEqualityComparer.Default.Equals(paramType, TypeLookup.MondState))
        {
            param.Type = ParameterType.State;
            param.TypeName = "state";
        }
        else if (paramType.TryGetAttribute("MondClassAttribute", out var mondClass))
        {
            param.Type = ParameterType.Value;
            param.TypeName = mondClass.GetArgument<string>() ?? paramType.Name;
            param.MondTypes = ObjectTypes;
            param.UserDataType = info.Type;
        }
        else
        {
            param.Type = ParameterType.Unsupported;
            param.TypeName = "unknown";

            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnsupportedParameterType, info.Locations.First(), info.Type.GetFullyQualifiedName()));
        }

        return param;
    }
}
