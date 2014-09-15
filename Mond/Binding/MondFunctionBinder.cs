using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mond.Binding
{
    public static partial class MondFunctionBinder
    {
        /// <summary>
        /// Generates a MondFunction binding for the given method.
        /// </summary>
        /// <param name="moduleName">Module name shown in errors. Can be null.</param>
        /// <param name="methodName">Method name shown in errors.</param>
        /// <param name="method">The function to bind.</param>
        public static MondFunction Bind(string moduleName, string methodName, MethodInfo method)
        {
            if (!method.IsStatic)
                throw new Exception("Bind only supports static methods");

            return BindImpl<MondFunction>(moduleName, methodName, method, false, (p, a, r) => BindFunctionCall(method, null, false, p, a, r));
        }

        internal static MondInstanceFunction BindInstance(string className, string methodName, Type type, MethodInfo method)
        {
            if (className == null)
                throw new ArgumentNullException("className");

            if (method.IsStatic)
                throw new Exception("BindInstance only supports instance methods");

            return BindImpl<MondInstanceFunction>(className, methodName, method, true, (p, a, r) => BindFunctionCall(method, type, true, p, a, r));
        }

        internal static MondFunction BindConstructor(string className, ConstructorInfo constructor, MondValue prototype)
        {
            if (className == null)
                throw new ArgumentNullException("className");

            return BindImpl<MondFunction>(className, "#ctor", constructor, false, (p, a, r) => BindConstructorCall(constructor, prototype, a, r));
        }

        private delegate Expression BindCallFactory(List<ParameterExpression> parameters, List<Expression> arguments, LabelTarget returnLabel);

        private static T BindImpl<T>(string moduleName, string methodName, MethodBase method, bool instanceFunction, BindCallFactory callFactory)
        {
            // TODO: clean up everything below this line

            var arguments = GetArguments(method, instanceFunction).ToArray();

            var errorPrefix = string.Format("{0}{1}{2}: ", moduleName ?? "", moduleName != null ? "." : "", methodName);

            var parameters = new List<ParameterExpression>
            {
                Expression.Parameter(typeof(MondState), "state"),
                Expression.Parameter(typeof(MondValue[]), "arguments")
            };

            if (instanceFunction)
                parameters.Insert(1, Expression.Parameter(typeof(MondValue), "instance"));

            var argumentsParam = parameters[instanceFunction ? 2 : 1];

            Func<int, Expression> argumentIndex = i => Expression.ArrayIndex(argumentsParam, Expression.Constant(i));

            var statements = new List<Expression>();
            var returnLabel = Expression.Label(typeof(MondValue));

            // argument count check
            var argLength = Expression.Condition(Expression.Equal(argumentsParam, Expression.Constant(null)), Expression.Constant(0), Expression.PropertyOrField(argumentsParam, "Length"));
            var requiredArgLength = arguments.Count(a => a.Index >= 0);
            var argLengthError = string.Format("{0}must be called with {1} argument{2}", errorPrefix, requiredArgLength, requiredArgLength != 1 ? "s" : "");

            statements.Add(Expression.IfThen(Expression.NotEqual(argLength, Expression.Constant(requiredArgLength)), Throw(argLengthError)));

            // argument type checks
            for (var i = 0; i < arguments.Length; i++)
            {
                var arg = arguments[i];

                if (arg.Index < 0 || arg.Type == typeof(MondValue) || arg.Type == typeof(MondState))
                    continue;

                statements.Add(TypeCheck(errorPrefix, i + 1, argumentIndex(arg.Index), arg.Type));
            }

            // call
            var callArgs = new List<Expression>();

            foreach (var arg in arguments)
            {
                if (arg.Type == typeof(MondState))
                {
                    callArgs.Add(parameters[0]);
                    continue;
                }

                if (arg.Type == typeof(MondValue))
                {
                    if (instanceFunction && arg.Name == "instance")
                        callArgs.Add(parameters[1]);
                    else
                        callArgs.Add(argumentIndex(arg.Index));

                    continue;
                }

                var input = argumentIndex(arg.Index);

                Func<Expression, Expression> inputConversion;
                if (ConversionMap.TryGetValue(arg.Type, out inputConversion))
                    input = inputConversion(input);

                callArgs.Add(Expression.Convert(input, arg.Type));
            }

            statements.Add(callFactory(parameters, callArgs, returnLabel));

            // end / default return
            statements.Add(Expression.Label(returnLabel, Expression.Constant(MondValue.Undefined)));

            var block = Expression.Block(statements);
            return Expression.Lambda<T>(block, parameters).Compile();
        }

        private static Expression BindFunctionCall(MethodInfo method, Type instanceType, bool instanceFunction, List<ParameterExpression> parameters, IEnumerable<Expression> arguments, LabelTarget returnLabel)
        {
            var returnType = method.ReturnType;

            Expression callExpr;

            if (instanceFunction && instanceType != null)
            {
                // instance functions store the instance in UserData
                var userData = Expression.Convert(Expression.PropertyOrField(parameters[1], "UserData"), instanceType);
                callExpr = Expression.Call(userData, method, arguments);
            }
            else
            {
                callExpr = Expression.Call(method, arguments);
            }

            if (returnType != typeof(void))
            {
                var output = callExpr;

                Func<Expression, Expression> outputConversion;
                if (ConversionMap.TryGetValue(returnType, out outputConversion))
                    output = outputConversion(output);

                callExpr = Expression.Return(returnLabel, Expression.Convert(output, typeof(MondValue)));
            }

            return callExpr;
        }

        private static Expression BindConstructorCall(ConstructorInfo constructor, MondValue prototype, IEnumerable<Expression> arguments, LabelTarget returnLabel)
        {
            var valueConstructor = typeof(MondValue).GetConstructor(new[] { typeof(MondValueType) });

            if (valueConstructor == null)
                throw new Exception("Could not find MondValue constructor");

            var objVar = Expression.Variable(typeof(MondValue));

            var createObj = Expression.Assign(objVar, Expression.New(valueConstructor, Expression.Constant(MondValueType.Object)));
            var setObjData = Expression.Assign(Expression.PropertyOrField(objVar, "UserData"), Expression.New(constructor, arguments));
            var setObjProto = Expression.Assign(Expression.PropertyOrField(objVar, "Prototype"), Expression.Constant(prototype));
            var retObj = Expression.Return(returnLabel, objVar);

            return Expression.Block(new[] { objVar }, createObj, setObjData, setObjProto, retObj);
        }

        private static Expression TypeCheck(string errorPrefix, int index, Expression argument, Type type)
        {
            MondValueType[] mondTypes;
            if (!TypeCheckMap.TryGetValue(type, out mondTypes))
                throw new Exception("Unsupported type " + type);

            var condition = Expression.NotEqual(Expression.PropertyOrField(argument, "Type"), Expression.Constant(mondTypes[0]));

            for (var i = 1; i < mondTypes.Length; i++)
            {
                condition = Expression.AndAlso(condition, Expression.NotEqual(Expression.PropertyOrField(argument, "Type"), Expression.Constant(mondTypes[i])));
            }

            var error = string.Format("{0}argument {1} must be of type {2}", errorPrefix, index, mondTypes[0].GetName());
            return Expression.IfThen(condition, Throw(error));
        }

        private class FunctionArgument
        {
            public readonly int Index;
            public readonly string Name;
            public readonly Type Type;

            public FunctionArgument(int index, string name, Type type)
            {
                Index = index;
                Name = name;
                Type = type;
            }
        }

        private static IEnumerable<FunctionArgument> GetArguments(MethodBase method, bool instanceFunction)
        {
            var index = 0;

            foreach (var p in method.GetParameters())
            {
                var skip = p.ParameterType == typeof(MondState) || (instanceFunction && p.ParameterType == typeof(MondValue) && p.Name == "instance");

                yield return new FunctionArgument(skip ? -1 : index, p.Name, p.ParameterType);

                if (!skip)
                    index++;
            }
        }

        private static Expression Throw(string message)
        {
            var constructor = typeof(MondRuntimeException).GetConstructor(new[] { typeof(string), typeof(bool) });

            if (constructor == null)
                throw new Exception("Could not find exception constructor");

            return Expression.Throw(Expression.New(constructor, Expression.Constant(message), Expression.Constant(false)));
        }
    }
}
