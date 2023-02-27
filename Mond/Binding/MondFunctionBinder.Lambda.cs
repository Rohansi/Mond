using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Mond.Binding
{
    public static partial class MondFunctionBinder
    {
        private static MondFunction BindImpl(string moduleName, MethodTable method, string nameOverride = null)
        {
            var errorPrefix = BindingError.ErrorPrefix(moduleName, nameOverride ?? method.Name);

            return (state, args) =>
            {
                MethodBase function;
                ReturnConverter returnConversion;
                var parameters = BuildParameterArray(errorPrefix, method, false, state, args, out _, out function, out returnConversion);

                return returnConversion(errorPrefix, state, Call(() => function.Invoke(null, parameters)));
            };
        }

        private static MondFunction BindInstanceImpl(string className, MethodTable method, string nameOverride = null, bool fakeInstance = false)
        {
            var errorPrefix = BindingError.ErrorPrefix(className, nameOverride ?? method.Name);

            if (!fakeInstance)
            {
                return (state, args) =>
                {
                    MethodBase function;
                    ReturnConverter returnConversion;
                    var parameters = BuildParameterArray(errorPrefix, method, true, state, args, out var instance, out function, out returnConversion);

                    var classInstance = instance.UserData;

                    if (ReferenceEquals(classInstance, null))
                        throw new MondRuntimeException(errorPrefix + BindingError.RequiresInstance);

                    return returnConversion(errorPrefix, state, Call(() => function.Invoke(classInstance, parameters)));
                };
            }

            return (state, args) =>
            {
                MethodBase function;
                ReturnConverter returnConversion;
                var parameters = BuildParameterArray(errorPrefix, method, true, state, args, out _, out function, out returnConversion);

                return returnConversion(errorPrefix, state, Call(() => function.Invoke(null, parameters)));
            };
        }

        private static MondConstructor BindConstructorImpl(string moduleName, MethodTable method)
        {
            var errorPrefix = BindingError.ErrorPrefix(moduleName, "#ctor");
            
            return (state, instance, args) =>
            {
                MethodBase constructor;
                ReturnConverter returnConversion;
                var parameters = BuildParameterArray(errorPrefix, method, false, state, args, out _, out constructor, out returnConversion);

                return Call(() => ((ConstructorInfo)constructor).Invoke(parameters));
            };
        }

        private static T Call<T>(Func<T> action)
        {
            try
            {
                return action();
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException == null)
                    throw;

                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                throw e.InnerException; // shouldnt reach this
            }
        }

        private static object[] BuildParameterArray(
            string errorPrefix,
            MethodTable methodTable,
            bool expectsInstance,
            MondState state,
            MondValue[] args,
            out MondValue instance,
            out MethodBase methodBase,
            out ReturnConverter returnConversion)
        {
            instance = MondValue.Undefined;
            
            Method method = null;

            var argCount = args.Length - (expectsInstance ? 1 : 0);
            if (argCount < methodTable.Methods.Count)
                method = FindMatch(methodTable.Methods[argCount], args, expectsInstance);

            if (method == null)
                method = FindMatch(methodTable.ParamsMethods, args, expectsInstance);

            if (method != null)
            {
                methodBase = method.Info;
                returnConversion = method.ReturnConversion;

                var j = 0;
                if (expectsInstance)
                {
                    if (args.Length == 0)
                        throw new MondRuntimeException(errorPrefix + BindingError.RequiresInstance);

                    instance = args[j++]; // first argument is the instance
                }

                var parameters = method.Parameters;
                var result = new object[parameters.Count];

                for (var i = 0; i < result.Length; i++)
                {
                    var param = parameters[i];

                    switch (param.Type)
                    {
                        case ParameterType.Value:
                            if (j < args.Length)
                                result[i] = param.Conversion(args[j++]);
                            else
                                result[i] = param.Info.DefaultValue;
                            break;

                        case ParameterType.Params:
                            result[i] = Slice(args, method.MondParameterCount);
                            break;

                        case ParameterType.Instance:
                            result[i] = instance;
                            break;

                        case ParameterType.State:
                            result[i] = state;
                            break;

                        default:
                            throw new NotSupportedException();
                    }
                }

                return result;
            }

            throw new MondBindingException(BindingError.ParameterTypeError(errorPrefix, methodTable));
        }

        private static Method FindMatch(List<Method> methods, MondValue[] args, bool expectsInstance)
        {
            var argOffset = expectsInstance ? 1 : 0;
            var argCount = expectsInstance ? args.Length - 1 : args.Length;

            for (var i = 0; i < methods.Count; i++)
            {
                var method = methods[i];
                var parameters = method.ValueParameters;

                if (method.RequiredMondParameterCount == 0 && (args.Length == argOffset || method.HasParams))
                    return methods[i];

                for (var j = 0; j < parameters.Count; j++)
                {
                    var param = parameters[j];

                    if (!(param.IsOptional && j >= argCount) && param.MondTypes[0] != MondValueType.Undefined)
                    {
                        var arg = args[argOffset + j];

                        if (!param.MondTypes.Contains(arg.Type))
                            break;

                        if (param.UserDataType != null && !param.UserDataType.IsInstanceOfType(arg.UserData))
                            break;
                    }

                    if (j == parameters.Count - 1)
                        return method;
                }
            }

            return null;
        }

        internal static Func<MondValue, object> MakeParameterConversion(Type parameterType)
        {
            if (parameterType == typeof(string))
                return v => (string)v;

            if (parameterType == typeof(bool))
                return v => (bool)v;

            if (parameterType == typeof(double))
                return v => (double)v;

            // cant use the Convert class for the rest of these...

            if (parameterType == typeof(float))
                return v => (float)v;

            if (parameterType == typeof(int))
                return v => (int)v;

            if (parameterType == typeof(uint))
                return v => (uint)v;

            if (parameterType == typeof(short))
                return v => (short)v;

            if (parameterType == typeof(ushort))
                return v => (ushort)v;

            if (parameterType == typeof(sbyte))
                return v => (sbyte)v;

            if (parameterType == typeof(byte))
                return v => (byte)v;

            return null;
        }

        internal static ReturnConverter MakeReturnConversion(Type returnType)
        {
            if (returnType == typeof(void))
                return (e, s, o) => MondValue.Undefined;

            if (returnType == typeof(MondValue))
                return (e, s, o) => ReferenceEquals(o, null) ? MondValue.Null : (MondValue)o;

            if (returnType == typeof(string))
                return (e, s, o) => ReferenceEquals(o, null) ? MondValue.Null : (MondValue)(string)o;

            if (returnType == typeof(bool))
                return (e, s, o) => (bool)o;

            if (NumberTypes.Contains(returnType))
                return (e, s, o) => Convert.ToDouble(o);

            var classAttrib = returnType.Attribute<MondClassAttribute>();
            if (classAttrib != null && classAttrib.AllowReturn)
            {
                var className = classAttrib.Name ?? returnType.Name;

                return (e, s, o) =>
                {
                    var prototype = s.FindPrototype(className);
                    if (prototype == MondValue.Undefined)
                        throw new MondRuntimeException(e + string.Format(BindingError.PrototypeNotFound, className));

                    var obj = MondValue.Object(s);
                    obj.Prototype = prototype;
                    obj.UserData = o;
                    return obj;
                };
            }

            throw new MondBindingException(BindingError.UnsupportedReturnType, returnType);
        }
    }
}
