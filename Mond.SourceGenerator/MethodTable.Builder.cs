using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Mond.SourceGenerator;

internal partial class MethodTable
{
    #region MethodTables

    public static List<MethodTable> Build(GeneratorExecutionContext context, IEnumerable<(IMethodSymbol Method, string Name, string Identifier)> source)
    {
        return source
            .GroupBy(m => m.Name)
            .Select(g => BuildMethodTable(context, g.Select(m => new Method(context, g.Key, m.Identifier, m.Method))))
            .ToList();
    }

    private static MethodTable BuildMethodTable(GeneratorExecutionContext context, IEnumerable<Method> source)
    {
        var sourceList = source.ToList();

        string name = null;
        string identifier = null;
        var methods = new List<List<Method>>();
        var paramsMethods = new List<Method>();

        foreach (var method in sourceList)
        {
            if (name == null || identifier == null)
            {
                name = method.Name;
                identifier = method.Identifier;
            }

            if (method.HasParams)
            {
                paramsMethods.Add(method);
            }

            for (var i = method.RequiredMondParameterCount; i <= method.MondParameterCount; i++)
            {
                while (methods.Count <= i)
                {
                    methods.Add(new List<Method>());
                }

                methods[i].Add(method);
            }
        }

        for (var i = 0; i < methods.Count; i++)
        {
            methods[i].Sort();
            methods[i] = methods[i].Distinct(new MethodParameterEqualityComparer(i)).ToList();
        }

        paramsMethods.Sort();

        // make sure all functions made it in
        var sourceMethodInfo = sourceList.Select(m => m.Info);

        var tableMethodInfo = methods.SelectMany(l => l).Select(m => m.Info);
        var paramsMethodInfo = paramsMethods.Select(m => m.Info);

        var difference = sourceMethodInfo
            .Except(tableMethodInfo.Concat(paramsMethodInfo))
            .ToList();

        foreach (var method in difference)
        {
            var methodName = method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundMethodOverloadConflicts, method.Locations.First(), methodName));
        }

        return new MethodTable(name, identifier, methods, paramsMethods);
    }

    private class MethodParameterEqualityComparer : IEqualityComparer<Method>
    {
        private readonly int _length;

        public MethodParameterEqualityComparer(int length)
        {
            _length = length;
        }

        public bool Equals(Method x, Method y)
        {
            var xParams = x.Parameters.Where(p => p.Type == ParameterType.Value || p.Type == ParameterType.Params).ToList();
            var yParams = y.Parameters.Where(p => p.Type == ParameterType.Value || p.Type == ParameterType.Params).ToList();

            for (var i = 0; i < _length; i++)
            {
                if (!xParams[i].MondTypes.SequenceEqual(yParams[i].MondTypes) ||
                    xParams[i].IsOptional != yParams[i].IsOptional)
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(Method obj)
        {
            return 0;
        }
    }

    #endregion
}
