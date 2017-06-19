#if !NO_EXPRESSIONS

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
        /// Creates the function call needed for a function binding.
        /// </summary>
        private delegate Expression BindCallFactory(
            string errorPrefix,
            Method method,
            List<ParameterExpression> parameters,
            List<Expression> arguments,
            LabelTarget returnLabel);

        private static TFunc BindImpl<TFunc, TReturn>(
            string moduleName,
            MethodTable methodTable,
            bool instanceFunction,
            BindCallFactory callFactory)
        {
            var errorPrefix = BindingError.ErrorPrefix(moduleName, methodTable.Name);

            var parameters = new List<ParameterExpression>
            {
                Expression.Parameter(typeof(MondState), "state"),
                Expression.Parameter(typeof(MondValue[]), "arguments")
            };

            if (instanceFunction)
                parameters.Insert(1, Expression.Parameter(typeof(MondValue), "instance"));

            var argumentsParam = parameters[instanceFunction ? 2 : 1];

            var statements = new List<Expression>();
            var returnLabel = Expression.Label(typeof(TReturn));

            var argumentsLength = Expression.PropertyOrField(argumentsParam, "Length");

            for (var i = 0; i < methodTable.Methods.Count; i++)
            {
                var dispatches = BuildDispatchExpression(errorPrefix, methodTable.Methods[i], i, parameters, instanceFunction, returnLabel, callFactory);

                if (dispatches.Count == 0)
                    continue;

                var requiredArgCount = Expression.Constant(i);
                var argLengthEqual = Expression.Equal(argumentsLength, requiredArgCount);
                var argBranch = Expression.IfThen(argLengthEqual, Expression.Block(dispatches));

                statements.Add(argBranch);
            }

            foreach (var group in methodTable.ParamsMethods.GroupBy(p => p.RequiredMondParameterCount))
            {
                var dispatches = BuildDispatchExpression(errorPrefix, group, int.MaxValue, parameters, instanceFunction, returnLabel, callFactory);
                var requiredArgCount = Expression.Constant(group.Key);
                var argLengthAtLeast = Expression.GreaterThanOrEqual(argumentsLength, requiredArgCount);
                var argBranch = Expression.IfThen(argLengthAtLeast, Expression.Block(dispatches));

                statements.Add(argBranch);
            }

            statements.Add(ThrowParameterTypeError(errorPrefix, methodTable));

            statements.Add(Expression.Label(returnLabel, Expression.Default(typeof(TReturn))));

            var block = Expression.Block(statements);
            return Expression.Lambda<TFunc>(block, parameters).Compile();
        }

        private static List<Expression> BuildDispatchExpression(
            string errorPrefix,
            IEnumerable<Method> methods,
            int checkedArguments,
            List<ParameterExpression> parameters,
            bool instanceFunction,
            LabelTarget returnLabel,
            BindCallFactory callFactory)
        {
            var stateParam = parameters[0];
            var argumentsParam = parameters[instanceFunction ? 2 : 1];
            var instanceParam = parameters[instanceFunction ? 1 : 0];

            Func<int, Expression> argumentIndex = i =>
                Expression.ArrayIndex(argumentsParam, Expression.Constant(i));

            var result = new List<Expression>();

            foreach (var method in methods)
            {
                Expression typeCondition = Expression.Constant(true);

                if (checkedArguments > 0 && method.ValueParameters.Count > 0)
                {
                    typeCondition = method.ValueParameters
                        .Take(checkedArguments)
                        .Select((p, i) => TypeCheck(argumentIndex(i), p))
                        .Aggregate(Expression.AndAlso);
                }

                var arguments = new List<Expression>();

                var j = 0;
                foreach (var param in method.Parameters)
                {
                    switch (param.Type)
                    {
                        case ParameterType.Value:
                            if (j < checkedArguments)
                                arguments.Add(param.Conversion(argumentIndex(j++)));
                            else
                                arguments.Add(Expression.Constant(param.Info.DefaultValue, param.Info.ParameterType));
                            break;

                        case ParameterType.Params:
                            var sliceMethod = typeof(MondFunctionBinder).GetMethod("Slice", BindingFlags.NonPublic | BindingFlags.Static);
                            arguments.Add(Expression.Call(sliceMethod, argumentsParam, Expression.Constant(method.RequiredMondParameterCount)));
                            break;

                        case ParameterType.Instance:
                            arguments.Add(instanceParam);
                            break;

                        case ParameterType.State:
                            arguments.Add(stateParam);
                            break;

                        default:
                            throw new NotSupportedException();
                    }
                }

                var callExpression = callFactory(errorPrefix, method, parameters, arguments, returnLabel);

                result.Add(Expression.IfThen(typeCondition, callExpression));
            }

            return result;
        }

        /// <summary>
        /// Creates an expression that checks an argument against its expected type.
        /// </summary>
        private static Expression TypeCheck(Expression argument, Parameter parameter)
        {
            var types = parameter.MondTypes;
            var argumentType = Expression.PropertyOrField(argument, "Type");

            if (types.Length == 0 || types[0] == MondValueType.Undefined)
                return Expression.Constant(true);

            if (types[0] == MondValueType.Object && parameter.UserDataType != null)
            {
                var isObject = Expression.Equal(argumentType, Expression.Constant(MondValueType.Object));
                var userData = Expression.PropertyOrField(argument, "UserData");
                var isCorrectType = Expression.TypeIs(userData, parameter.UserDataType);
                return Expression.AndAlso(isObject, isCorrectType);
            }

            return types
                .Select(t => Expression.Equal(argumentType, Expression.Constant(t)))
                .Aggregate(Expression.OrElse);
        }

        private static Expression ThrowParameterTypeError(string errorPrefix, MethodTable methodTable)
        {
            var parameterTypeError = typeof(BindingError).GetMethod("ParameterTypeError", BindingFlags.Public | BindingFlags.Static);
            var errorString = Expression.Call(parameterTypeError, Expression.Constant(errorPrefix), Expression.Constant(methodTable));

            return Expression.Throw(Expression.New(RuntimeExceptionConstructor, errorString));
        }

        /// <summary>
        /// Creates the function call for normal function calls. Should be used through BindCallFactory.
        /// </summary>
        private static Expression BindFunctionCall(
            string errorPrefix,
            Method method,
            Type instanceType,
            bool instanceFunction,
            List<ParameterExpression> parameters,
            IEnumerable<Expression> arguments,
            LabelTarget returnLabel)
        {
            var returnType = ((MethodInfo)method.Info).ReturnType;
            var expressions = new List<Expression>();

            Expression callExpr;
            if (instanceFunction && instanceType != null)
            {
                // instance functions store the instance in UserData
                var userData = Expression.PropertyOrField(parameters[1], "UserData");

                var throwExpr = Expression.Throw(Expression.New(RuntimeExceptionConstructor,
                    Expression.Constant(errorPrefix + BindingError.RequiresInstance)));

                expressions.Add(Expression.IfThen(Expression.ReferenceEqual(userData, Expression.Constant(null)), throwExpr));

                var userDataCasted = Expression.Convert(userData, instanceType);
                callExpr = Expression.Call(userDataCasted, (MethodInfo)method.Info, arguments);
            }
            else
            {
                callExpr = Expression.Call((MethodInfo)method.Info, arguments);
            }

            if (returnType != typeof(void))
            {
                expressions.Add(Expression.Return(returnLabel, method.ReturnConversion(errorPrefix, parameters[0], callExpr)));
            }
            else
            {
                expressions.Add(callExpr);
                expressions.Add(Expression.Return(returnLabel, Expression.Constant(MondValue.Undefined)));
            }

            return Expression.Block(expressions);
        }

        /// <summary>
        /// Creates the function call for a constructor function. Should be used through BindCallFactory.
        /// </summary>
        private static Expression BindConstructorCall(Method method, IEnumerable<Expression> arguments, LabelTarget returnLabel)
        {
            var constructor = (ConstructorInfo)method.Info;
            return Expression.Return(returnLabel, Expression.New(constructor, arguments));
        }

        // MondValue -> T
        internal static Func<Expression, Expression> MakeParameterConversion(Type parameterType)
        {
            if (BasicTypes.Contains(parameterType))
                return e => Expression.Convert(e, parameterType);

            if (NumberTypes.Contains(parameterType))
                return e => Expression.Convert(Expression.Convert(e, typeof(double)), parameterType);

            return null;
        }

        // T -> MondValue
        internal static ReturnConverter MakeReturnConversion(Type returnType)
        {
            if (returnType == typeof(void))
                return (e, s, v) => throw new MondBindingException("Expression binder should not use void return conversion");

            if (returnType == typeof(MondValue))
                return (e, s, v) => v;

            if (returnType == typeof(string))
            {
                return (e, s, v) =>
                {
                    var tempVar = Expression.Variable(typeof(string));

                    var tempVarIsNull = Expression.ReferenceEqual(tempVar, Expression.Constant(null));
                    var tempVarAsValue = Expression.Convert(tempVar, typeof(MondValue));

                    var block = Expression.Block(
                        new[] { tempVar },
                        Expression.Assign(tempVar, v),
                        Expression.Condition(tempVarIsNull, Expression.Constant(MondValue.Null), tempVarAsValue)
                    );

                    return block;
                };
            }

            if (BasicTypes.Contains(returnType))
                return (e, s, v) => Expression.Convert(v, typeof(MondValue));

            if (NumberTypes.Contains(returnType))
                return (e, s, v) => Expression.Convert(Expression.Convert(v, typeof(double)), typeof(MondValue));

            var classAttrib = returnType.Attribute<MondClassAttribute>();
            if (classAttrib != null && classAttrib.AllowReturn)
            {
                var objInitializerType = typeof(IEnumerable<KeyValuePair<MondValue, MondValue>>);
                var valueFactory = typeof(MondValue).GetMethod("Object", new[] { typeof(MondState), objInitializerType });
                if (valueFactory == null)
                    throw new Exception("Could not find MondValue.Object method");

                var findPrototype = typeof(MondState).GetMethod("FindPrototype");
                if (findPrototype == null)
                    throw new Exception("Could not find FindPrototype method");

                var className = classAttrib.Name ?? returnType.Name;

                return (e, s, v) =>
                {
                    var prototype = Expression.Variable(typeof(MondValue));
                    var obj = Expression.Variable(typeof(MondValue));

                    var throwMsg = Expression.Constant(e + string.Format(BindingError.PrototypeNotFound, className));
                    var throwExpr = Expression.Throw(Expression.New(RuntimeExceptionConstructor, throwMsg));

                    return Expression.Block(
                        new [] { prototype, obj },
                        Expression.Assign(prototype, Expression.Call(s, findPrototype, Expression.Constant(className))),
                        Expression.IfThen(Expression.Equal(prototype, Expression.Constant(MondValue.Undefined)), throwExpr),
                        Expression.Assign(obj, Expression.Call(valueFactory, s, Expression.Constant(null, objInitializerType))),
                        Expression.Assign(Expression.PropertyOrField(obj, "Prototype"), prototype),
                        Expression.Assign(Expression.PropertyOrField(obj, "UserData"), v),
                        obj
                    );
                };
            }

            throw new MondBindingException(BindingError.UnsupportedReturnType, returnType);
        }
    }
}

#endif
