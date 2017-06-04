using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Mond.Binding
{
    static class BindingError
    {
        public const string UnsupportedType = "Unsupported type: {0}";
        public const string UnsupportedReturnType = "Unsupported return type: {0}";

        public const string TypeMissingAttribute = "Type must have the {0} attribute";

        public const string DuplicateDefinition = "Duplicate definition of '{0}'";

        public const string NameIsntValidOperator = "'{0}' is not a valid operator name";

        public const string RequiresInstance = "Must be called on an instance";

        public const string PrototypeNotFound = "Prototype for class '{0}' was not found";
        public const string DuplicatePrototype = "Prototype for class '{0}' was already found in the given state";

        public static string MethodsHiddenError(IEnumerable<MethodBase> methods)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Methods are hidden by overloads:");

            foreach (var m in methods)
            {
                sb.AppendLine(m.ToString());
            }

            return sb.ToString();
        }

        public static string ErrorPrefix(string moduleName, string methodName)
        {
            return string.Format("{0}{1}{2}: ", moduleName ?? "", string.IsNullOrEmpty(moduleName) ? "" : ".", methodName);
        }

        public static string ParameterTypeError(string prefix, MethodTable methodTable)
        {
            var sb = new StringBuilder();

            sb.Append(prefix);
            sb.AppendLine("argument types do not match any available functions");

            var methods = methodTable.Methods
                .SelectMany(l => l)
                .Concat(methodTable.ParamsMethods)
                .Distinct();

            foreach (var method in methods)
            {
                sb.Append("- ");
                sb.AppendLine(method.ToString());
            }

            return sb.ToString();
        }
    }
}
