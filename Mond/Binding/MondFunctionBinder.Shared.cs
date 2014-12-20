using System;
using System.Collections.Generic;
using System.Reflection;

namespace Mond.Binding
{
    public static partial class MondFunctionBinder
    {
        private static string GenerateErrorPrefix(string moduleName, string methodName)
        {
            return string.Format("{0}{1}{2}: ", moduleName ?? "", string.IsNullOrEmpty(moduleName) ? "" : ".", methodName);
        }

        private static string GenerateArgumentLengthError(string prefix, int requiredArgumentCount)
        {
            return string.Format("{0}must be called with {1} argument{2}", prefix, requiredArgumentCount, requiredArgumentCount != 1 ? "s" : "");
        }

        private static string GenerateTypeError(string prefix, int argumentIndex, string expectedType)
        {
            return string.Format("{0}argument {1} must be of type {2}", prefix, argumentIndex + 1, expectedType);
        }

        private class FunctionArgument
        {
            public readonly int Index;
            public readonly ParameterInfo Info;

            public Type Type { get { return Info.ParameterType; } }

            public FunctionArgument(int index, ParameterInfo info)
            {
                Index = index;
                Info = info;
            }
        }

        private static IEnumerable<FunctionArgument> GetArguments(MethodBase method, bool instanceFunction)
        {
            var parameters = method.GetParameters();
            var index = 0;

            for (var i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];

                var skip = (p.ParameterType == typeof(MondState)) ||
                           (instanceFunction && p.ParameterType == typeof(MondValue) && p.Attribute<MondInstanceAttribute>() != null) ||
                           (i == parameters.Length - 1 && p.ParameterType == typeof(MondValue[]) && p.Attribute<ParamArrayAttribute>() != null);

                yield return new FunctionArgument(skip ? -1 : index, p);

                if (!skip)
                    index++;
            }
        }
    }
}
