using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mond.Binding
{
    public static partial class MondFunctionBinder
    {
        #region MethodTables

        internal enum MethodType
        {
            Normal, Property, Constructor, Operator
        }

        internal static List<MethodTable> BuildMethodTables(IEnumerable<MethodBase> source, MethodType methodType, string nameOverride = null)
        {
            switch (methodType)
            {
                case MethodType.Normal:
                    {
                        return source
                            .Select(m => new { Method = m, FunctionAttribute = m.Attribute<MondFunctionAttribute>() })
                            .Where(m => m.FunctionAttribute != null)
                            .GroupBy(m => nameOverride ?? m.FunctionAttribute.Name ?? m.Method.Name.ToCamelCase())
                            .Select(g => BuildMethodTable(g.Select(m => new Method(g.Key, m.Method))))
                            .ToList();
                    }

                case MethodType.Property:
                    {
                        if (string.IsNullOrEmpty(nameOverride))
                            throw new ArgumentNullException(nameof(nameOverride));

                        return new List<MethodTable>
                        {
                            BuildMethodTable(source.Select(m => new Method(nameOverride, m)))
                        };
                    }

                case MethodType.Constructor:
                    {
                        var methods = source
                            .Where(m => m.Attribute<MondConstructorAttribute>() != null)
                            .ToList();

                        return new List<MethodTable>
                        {
                            BuildMethodTable(methods.Select(m => new Method("#ctor", m)))
                        };
                    }

                case MethodType.Operator:
                    {
                        return source
                            .Select(m => new { Method = m, OperatorAttribute = m.Attribute<MondOperatorAttribute>() })
                            .Where(m => m.OperatorAttribute != null)
                            .GroupBy(m => m.OperatorAttribute.Operator)
                            .Select(g => BuildMethodTable(g.Select(m => new Method(g.Key, m.Method))))
                            .ToList();
                    }

                default:
                    throw new NotSupportedException();
            }
        }

        private static MethodTable BuildMethodTable(IEnumerable<Method> source)
        {
            var sourceList = source.ToList();

            string name = null;
            var methods = new List<List<Method>>();
            var paramsMethods = new List<Method>();

            foreach (var method in sourceList)
            {
                if (name == null)
                {
                    name = method.Name;
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

            if (difference.Count > 0)
            {
                throw new MondBindingException(BindingError.MethodsHiddenError(difference));
            }

            return new MethodTable(name, methods, paramsMethods);
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

        internal static MondValue[] Slice(MondValue[] values, int index)
        {
            var result = new MondValue[values.Length - index];

            var j = 0;
            for (var i = index; i < values.Length; i++, j++)
            {
                result[j] = values[i];
            }

            return result;
        }
    }
}
