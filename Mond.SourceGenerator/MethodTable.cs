using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Mond.SourceGenerator;

internal partial class MethodTable
{
    public readonly string Name;
    public readonly List<List<Method>> Methods;
    public readonly List<Method> ParamsMethods;

    public MethodTable(string name, List<List<Method>> methods, List<Method> paramsMethods)
    {
        Name = name;

        Methods = methods;
        ParamsMethods = paramsMethods;
    }
}

internal class Method : IComparable<Method>
{
    public readonly IMethodSymbol Info;

    public readonly string Name;

    public readonly int MondParameterCount;         // maximum number of ParameterType.Value parameters
    public readonly int RequiredMondParameterCount; // number of required ParameterType.Value parameters

    public readonly List<Parameter> Parameters;
    public readonly List<Parameter> ValueParameters;

    public readonly bool HasParams;

    public Method(string name, IMethodSymbol info)
    {
        Name = name;
        Info = info;

        var parameters = info.Parameters;

        Parameters = parameters
            .Select(p => new Parameter(p))
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
    Value,
    Params,
    State,
    Instance
}

internal class Parameter
{
    private static readonly MondValueType[] AnyTypes = [MondValueType.Undefined];
    private static readonly MondValueType[] ObjectTypes = [MondValueType.Object];

    public readonly IParameterSymbol Info;

    public readonly ParameterType Type;
    public readonly string TypeName;

    public readonly bool IsOptional;

    public readonly int Priority;

    public readonly MondValueType[] MondTypes;

    public readonly ITypeSymbol UserDataType;

    public Parameter(IParameterSymbol info)
    {
        Info = info;

        IsOptional = info.IsOptional;

        var paramType = info.Type;

        if (TypeLookup.TypeCheckMap.TryGetValue(paramType, out var mondTypes))
        {
            Type = ParameterType.Value;
            TypeName = mondTypes[0].GetName();

            if (SymbolEqualityComparer.Default.Equals(paramType, TypeLookup.Bool))
            {
                Priority = 10;
            }
            else if (TypeLookup.NumberTypes.Contains(paramType))
            {
                Priority = 20;
            }
            else if (SymbolEqualityComparer.Default.Equals(paramType, TypeLookup.String))
            {
                Priority = 30;
            }

            MondTypes = mondTypes;
            return;
        }

        if (SymbolEqualityComparer.Default.Equals(paramType, TypeLookup.MondValue))
        {
            if (info.HasAttribute("MondInstanceAttribute"))
            {
                Type = ParameterType.Instance;
                TypeName = "instance";
                return;
            }

            Type = ParameterType.Value;
            TypeName = "any";
            Priority = 100;
            MondTypes = AnyTypes;
            return;
        }

        if (SymbolEqualityComparer.Default.Equals(paramType, TypeLookup.MondValueNullable))
        {
            Type = ParameterType.Value;
            TypeName = "any?";
            Priority = 100;
            MondTypes = AnyTypes;
            return;
        }

        if (SymbolEqualityComparer.Default.Equals(paramType, TypeLookup.MondValueArray) && info.IsParams)
        {
            Type = ParameterType.Params;
            TypeName = "...";
            Priority = 75;
            return;
        }

        if (SymbolEqualityComparer.Default.Equals(paramType, TypeLookup.MondState))
        {
            Type = ParameterType.State;
            TypeName = "state";
            return;
        }

        if (paramType.TryGetAttribute("MondClassAttribute", out var mondClass))
        {
            Type = ParameterType.Value;
            TypeName = mondClass.GetArgument() ?? paramType.Name;
            MondTypes = ObjectTypes;
            UserDataType = info.Type;
            return;
        }

        throw new Exception("unsupported parameter type");
    }

    public override string ToString()
    {
        return TypeName;
    }
}
