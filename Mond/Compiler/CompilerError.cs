namespace Mond.Compiler
{
    static class CompilerError
    {
        public const string UnterminatedString = "Unterminated string";
        public const string UnexpectedEofComment = "Unexpected end of file (unterminated block comment)";
        public const string UnexpectedEofString = "Unexpected end of file (bad escape sequence)";
        public const string InvalidEscapeSequence = "Invalid escape sequence '{0}'";
        public const string InvalidNumber = "Invalid {0} number '{1}'";
        public const string EmptyNumber = "Invalid {0} number";
        public const string UnexpectedCharacter = "Unexpected character '{0}'";
        public const string ExpectedButFound = "Expected {0} but found {1}";
        public const string ExpectedButFound2 = "Expected {0} or {1} but found {2}";

        public const string IncorrectOperatorArity = "Incorrect operator arity ({0}). User defined operators may only accept 1 (prefix) or 2 (infix) arguments";
        public const string EllipsisInOperator = "User defined operators may not have ellipsis arguments";
        public const string CantNestOperatorDecl = "User defined operators may only be declared in the top-level scope";
        public const string CantOverrideBuiltInOperator = "Cannot define operator '{0}', as it would shadow the built-in '{0}' operator";

        public const string UndefinedIdentifier = "Undefined identifier '{0}'";
        public const string IdentifierAlreadyDefined = "Identifier '{0}' was previously defined in this scope";
        public const string CantModifyReadonlyVar = "Cannot modify '{0}' because it is readonly";
        public const string LeftSideMustBeStorable = "The left side of an assignment must be storable";

        public const string ExpectedConstant = "Expected a constant value";
        public const string DuplicateCase = "Duplicate case value";
        public const string DuplicateDefault = "Cannot have more than one default case";

        public const string MultipleDestructuringSlices = "Cannot have multiple ellipses in array destructuring";

        public const string BadForLoopInitializer = "For loop initializer cannot be statement";

        public const string ObjectFunctionNotNamed = "Object literal functions must be named";

        public const string UnresolvedJump = "Unresolved jump";

        public const string YieldInFun = "'yield' cannot be used in functions";

        public const string PipelineNeedsCall = "The right side of the pipeline operator must be a function call";

        public const string UnpackMustBeInCall = "The unpack operator can only be used as a function call argument";
        public const string TooManyUnpacks = "Cannot have more than 255 unpacks in a single function call";

        public const string FailedToDefineInternal = "Failed to define internal variable (compiler bug)";
        public const string BadStackState = "Bad stack state (compiler bug)";
    }
}
