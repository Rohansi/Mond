using System;
using System.Linq;
using System.Reflection;

namespace Mond.Binding
{
#if NO_EXPRESSIONS
    public static partial class MondFunctionBinder
    {
        private static MondFunction BindImpl(string moduleName, string methodName, MethodInfo method)
        {
            var errorPrefix = GenerateErrorPrefix(moduleName, methodName);
            int requiredArgsLength;
            var argumentTypes = GetArgumentInfo(method, false, out requiredArgsLength);
            var hasParams = argumentTypes.Any(a => a.SpecialType == SpecialArgumentType.Params);
            var returnConversion = MakeReturnConversion(method.ReturnType);

            return (state, args) =>
                returnConversion(method.Invoke(null, BuildArgumentArray(errorPrefix, argumentTypes, requiredArgsLength, hasParams, state, null, args)));
        }

        private static MondInstanceFunction BindInstanceImpl(string moduleName, string methodName, MethodInfo method)
        {
            var errorPrefix = GenerateErrorPrefix(moduleName, methodName);
            int requiredArgsLength;
            var argumentTypes = GetArgumentInfo(method, true, out requiredArgsLength);
            var hasParams = argumentTypes.Any(a => a.SpecialType == SpecialArgumentType.Params);
            var returnConversion = MakeReturnConversion(method.ReturnType);

            return (state, instance, args) =>
            {
                var classInstance = instance.UserData;
                var parameters = BuildArgumentArray(errorPrefix, argumentTypes, requiredArgsLength, hasParams, state, instance, args);
                return returnConversion(method.Invoke(classInstance, parameters));
            };
        }

        private static MondConstructor BindConstructorImpl(string moduleName, ConstructorInfo method)
        {
            var errorPrefix = GenerateErrorPrefix(moduleName, "#ctor");
            int requiredArgsLength;
            var argumentTypes = GetArgumentInfo(method, true, out requiredArgsLength);
            var hasParams = argumentTypes.Any(a => a.SpecialType == SpecialArgumentType.Params);

            return (state, instance, args) =>
                method.Invoke(BuildArgumentArray(errorPrefix, argumentTypes, requiredArgsLength, hasParams, state, instance, args));
        }

        private static object[] BuildArgumentArray(
            string errorPrefix,
            ArgumentInfo[] argumentTypes,
            int requiredArgsLength,
            bool hasParams,
            MondState state,
            MondValue instance,
            MondValue[] args)
        {
            if ((hasParams && args.Length < requiredArgsLength) || (!hasParams && args.Length != requiredArgsLength))
                throw new MondRuntimeException(GenerateArgumentLengthError(errorPrefix, requiredArgsLength));

            var result = new object[argumentTypes.Length];

            for (var i = 0; i < result.Length; i++)
            {
                var argType = argumentTypes[i];

                if (argType.Index >= 0)
                {
                    var value = args[argType.Index];
                    var types = argType.MondTypes;

                    if (types[0] != MondValueType.Undefined && !types.Contains(value.Type))
                        throw new MondRuntimeException(GenerateTypeError(errorPrefix, argType.Index, types[0].GetName()));

                    result[i] = argType.Conversion(value);
                }
                else
                {
                    switch (argType.SpecialType)
                    {
                        case SpecialArgumentType.State:
                            result[i] = state;
                            break;

                        case SpecialArgumentType.Instance:
                            result[i] = instance;
                            break;

                        case SpecialArgumentType.Params:
                            result[i] = args;
                            break;

                        default:
                            throw new NotSupportedException();
                    }
                }
            }

            return result;
        }

        private static Func<MondValue, object> MakeArgumentConversion(Type argumentType)
        {
            if (argumentType == typeof(string))
                return v => (string)v;

            if (argumentType == typeof(bool))
                return v => (bool)v;

            if (argumentType == typeof(double))
                return v => (double)v;

            // cant use the Convert class for the rest of these...

            if (argumentType == typeof(float))
                return v => (float)v;

            if (argumentType == typeof(int))
                return v => (int)v;

            if (argumentType == typeof(uint))
                return v => (uint)v;

            if (argumentType == typeof(short))
                return v => (short)v;

            if (argumentType == typeof(ushort))
                return v => (ushort)v;

            if (argumentType == typeof(sbyte))
                return v => (sbyte)v;

            if (argumentType == typeof(byte))
                return v => (byte)v;

            throw new MondBindingException(BindingError.UnsupportedType, argumentType);
        }

        private static Func<object, MondValue> MakeReturnConversion(Type returnType)
        {
            if (returnType == typeof(void))
                return o => MondValue.Undefined;

            if (returnType == typeof(MondValue))
                return o => (MondValue)o;

            if (returnType == typeof(string))
                return o => (string)o;

            if (returnType == typeof(bool))
                return o => (bool)o;

            if (NumberTypes.Contains(returnType))
                return o => Convert.ToDouble(o);

            throw new MondBindingException(BindingError.UnsupportedReturnType, returnType);
        }

        private enum SpecialArgumentType
        {
            None, State, Instance, Params
        }

        private class ArgumentInfo
        {
            private static readonly MondValueType[] AnyTypes = { MondValueType.Undefined };
            private static readonly MondValueType[] ObjectTypes = { MondValueType.Object };

            private readonly FunctionArgument _argument;

            public int Index { get { return _argument.Index; } }

            public Type Type { get { return _argument.Type; } }

            public MondValueType[] MondTypes { get; private set; }

            public Func<MondValue, object> Conversion { get; private set; }

            public SpecialArgumentType SpecialType { get; private set; }

            public ArgumentInfo(FunctionArgument argument)
            {
                _argument = argument;

                MondTypes = null;
                Conversion = null;
                SpecialType = SpecialArgumentType.None;

                if (Index >= 0)
                {
                    MondValueType[] mondTypes;

                    if (Type == typeof(MondValue))
                    {
                        MondTypes = AnyTypes;
                        Conversion = v => v;
                    }
                    else if (TypeCheckMap.TryGetValue(Type, out mondTypes))
                    {
                        MondTypes = mondTypes;
                        Conversion = MakeArgumentConversion(Type);
                    }
                    else if (Type.Attribute<MondClassAttribute>() != null)
                    {
                        MondTypes = ObjectTypes;
                        Conversion = v => v.UserData;
                    }
                    else
                    {
                        throw new MondBindingException(BindingError.UnsupportedType, Type);
                    }
                }
                else
                {
                    if (Type == typeof(MondState))
                    {
                        SpecialType = SpecialArgumentType.State;
                    }
                    else if (Type == typeof(MondValue))
                    {
                        SpecialType = SpecialArgumentType.Instance;
                    }
                    else if (Type == typeof(MondValue[]))
                    {
                        SpecialType = SpecialArgumentType.Params;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
            }
        }

        private static ArgumentInfo[] GetArgumentInfo(MethodBase method, bool instanceFunction, out int requiredArgsLength)
        {
            var arguments = GetArguments(method, instanceFunction).Select(a => new ArgumentInfo(a)).ToArray();

            if (arguments.Length == 0)
                requiredArgsLength = 0;
            else
                requiredArgsLength = arguments.Max(a => a.Index) + 1;

            return arguments;
        }
    }
#endif
}

