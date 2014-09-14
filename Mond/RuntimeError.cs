namespace Mond
{
    static class RuntimeError
    {
        public const string CantUseOperatorOnTypes = "Can not use {0} operator on {1} and {2}";
        public const string CantUseOperatorOnType = "Can not use {0} operator on {1}";

        public const string IndexOutOfBounds = "Index out of bounds";

        public const string CantCastTo = "{0} can not be casted to a {1}";

        public const string CantCreateField = "Fields can not be created on type {0}";

        public const string CircularPrototype = "Circular prototype definition";

        public const string ValueNotCallable = "Value of type {0} is not callable";

        public const string StackOverflow = "Stack overflow";

        public const string UnhandledOpcode = "Unhandled opcode (runtime bug)";
        public const string UnhandledClosureType = "Unhandled closure type (runtime bug)";

        public const string NegativeShift = "Can not shift by a negative amount.";
    }
}
