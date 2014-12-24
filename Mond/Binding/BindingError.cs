namespace Mond.Binding
{
    static class BindingError
    {
        public const string UnsupportedType = "Unsupported type: {0}";
        public const string UnsupportedReturnType = "Unsupported return type: {0}";

        public const string TypeMissingAttribute = "Type must have the {0} attribute";

        public const string DuplicateDefinition = "Duplicate definition of '{0}'";

        public const string TooManyConstructors = "Classes cannot have multiple Mond constructors";
        public const string NotEnoughConstructors = "Classes must have one Mond constructor";

        public static string ErrorPrefix(string moduleName, string methodName)
        {
            return string.Format("{0}{1}{2}: ", moduleName ?? "", string.IsNullOrEmpty(moduleName) ? "" : ".", methodName);
        }

        public static string ArgumentLengthError(string prefix, int requiredArgumentCount)
        {
            return string.Format("{0}must be called with {1} argument{2}", prefix, requiredArgumentCount, requiredArgumentCount != 1 ? "s" : "");
        }

        public static string TypeError(string prefix, int argumentIndex, string expectedType)
        {
            return string.Format("{0}argument {1} must be of type {2}", prefix, argumentIndex + 1, expectedType);
        }
    }
}
