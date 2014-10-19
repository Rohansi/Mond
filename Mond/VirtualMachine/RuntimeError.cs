namespace Mond.VirtualMachine
{
    static class RuntimeError
    {
        public const string CantUseOperatorOnTypes = "Cannot use {0} operator on {1} and {2}";
        public const string CantUseOperatorOnType = "Cannot use {0} operator on {1}";

        public const string IndexOutOfBounds = "Index out of bounds";

        public const string CantCastTo = "{0} cannot be casted to a {1}";

        public const string CantCreateField = "Fields cannot be created on type {0}";

        public const string CircularPrototype = "Circular prototype definition";

        public const string ValueNotCallable = "Value of type {0} is not callable";

        public const string NumberCastWrongType = "__number must return a number";
        public const string StringCastWrongType = "__string must return a string";
        public const string BoolCastWrongType = "__bool must return a bool";

        public const string StackOverflow = "Stack overflow";
        public const string StackEmpty = "Stack is empty (runtime bug)";

        public const string UnhandledOpcode = "Unhandled opcode (runtime bug)";
        public const string UnhandledClosureType = "Unhandled closure type (runtime bug)";
    }
}
