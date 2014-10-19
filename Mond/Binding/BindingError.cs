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
    }
}
