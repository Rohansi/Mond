using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

#if !NO_EXPRESSIONS
using System.Linq.Expressions;
#endif

namespace Mond.Binding
{
    internal class MethodTable
    {
        public string Name { get; }
        public List<List<Method>> Methods { get; }
        public List<Method> ParamsMethods { get; }

        public MethodTable(string name, List<List<Method>> methods, List<Method> paramsMethods)
        {
            Name = name;

            Methods = methods;
            ParamsMethods = paramsMethods;
        }
    }

#if !NO_EXPRESSIONS
    internal delegate Expression ReturnConverter(string errorPrefix, Expression state, Expression value);
#else
    internal delegate MondValue ReturnConverter(string errorPrefix, MondState state, object value);
#endif

    internal class Method : IComparable<Method>
    {
        public MethodBase Info { get; }

        public string Name { get; }

        public int MondParameterCount { get; }             // maximum number of ParameterType.Value parameters
        public int RequiredMondParameterCount { get; }     // number of required ParameterType.Value parameters

        public List<Parameter> Parameters { get; }
        public List<Parameter> ValueParameters { get; }

        public bool HasParams { get; }
        
        public ReturnConverter ReturnConversion { get; }

        public Method(string name, MethodBase info)
        {
            Name = name;
            Info = info;

            var parameters = info.GetParameters();

            Parameters = parameters
                .Select(p => new Parameter(p))
                .ToList();

            ValueParameters = Parameters
                .Where(p => p.Type == ParameterType.Value)
                .ToList();

            MondParameterCount = ValueParameters.Count;
            RequiredMondParameterCount = ValueParameters.Count(p => !p.IsOptional);

            HasParams = Parameters.Any(p => p.Type == ParameterType.Params);
            
            if (info is MethodInfo methodInfo)
                ReturnConversion = MondFunctionBinder.MakeReturnConversion(methodInfo.ReturnType);
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
        private static readonly MondValueType[] AnyTypes = { MondValueType.Undefined };
        private static readonly MondValueType[] ObjectTypes = { MondValueType.Object };

        public ParameterInfo Info { get; }

        public ParameterType Type { get; }
        public string TypeName { get; }

        public bool IsOptional { get; }

        public int Priority { get; }

        public MondValueType[] MondTypes { get; }

        public Type UserDataType { get; }

#if !NO_EXPRESSIONS
        public Func<Expression, Expression> Conversion { get; }
#else
        public Func<MondValue, object> Conversion { get; }
#endif

        public Parameter(ParameterInfo info)
        {
            Info = info;

            IsOptional = info.IsOptional;

            var paramType = info.ParameterType;

            if (MondFunctionBinder.TypeCheckMap.TryGetValue(paramType, out var mondTypes))
            {
                Type = ParameterType.Value;
                TypeName = mondTypes[0].GetName();

                if (paramType == typeof(bool))
                {
                    Priority = 10;
                }
                else if (MondFunctionBinder.NumberTypes.Contains(paramType))
                {
                    Priority = 20;
                }
                else if (paramType == typeof(string))
                {
                    Priority = 30;
                }

                Conversion = MondFunctionBinder.MakeParameterConversion(info.ParameterType);

                MondTypes = mondTypes;
                return;
            }

            if (paramType == typeof(MondValue))
            {
                if (info.Attribute<MondInstanceAttribute>() != null)
                {
                    Type = ParameterType.Instance;
                    TypeName = "instance";
                    return;
                }

                Type = ParameterType.Value;
                TypeName = "any";
                Priority = 100;
                MondTypes = AnyTypes;
                Conversion = v => v;
                return;
            }

            if (paramType == typeof(MondValue?))
            {
                Type = ParameterType.Value;
                TypeName = "any?";
                Priority = 100;
                MondTypes = AnyTypes;

#if !NO_EXPRESSIONS
                Conversion = v => Expression.Condition(Expression.Equal(v, Expression.Constant(MondValue.Undefined)), Expression.Constant(null, paramType), Expression.Convert(v, paramType));
#else
                Conversion = v => v == MondValue.Undefined ? null : (MondValue?)v;
#endif

                return;
            }

            if (paramType == typeof(MondValue[]) && info.Attribute<ParamArrayAttribute>() != null)
            {
                Type = ParameterType.Params;
                TypeName = "...";
                Priority = 75;
                return;
            }

            if (paramType == typeof(MondState))
            {
                Type = ParameterType.State;
                TypeName = "state";
                return;
            }

            MondClassAttribute mondClass;
            if ((mondClass = paramType.Attribute<MondClassAttribute>()) != null)
            {
                Type = ParameterType.Value;
                TypeName = mondClass.Name ?? paramType.Name;
                MondTypes = ObjectTypes;
                UserDataType = info.ParameterType;

#if !NO_EXPRESSIONS
                Conversion = v => Expression.Convert(Expression.PropertyOrField(v, "UserData"), info.ParameterType);
#else
                Conversion = v => v.UserData;
#endif

                return;
            }

            throw new MondBindingException(BindingError.UnsupportedType, info.ParameterType);
        }

        public override string ToString()
        {
            return TypeName;
        }
    }
}
