namespace Mond
{
    /// <summary>
    /// Cached <see cref="MondValue"/> keys for the metamethod (and proxy handler) names.
    /// </summary>
    internal static class Metamethod
    {
        // proxy handler methods
        public static readonly MondValue Get = "get";
        public static readonly MondValue Set = "set";

        public static readonly MondValue Add = "__add";
        public static readonly MondValue And = "__and";
        public static readonly MondValue Bool = "__bool";
        public static readonly MondValue Call = "__call";
        public static readonly MondValue Div = "__div";
        public static readonly MondValue Eq = "__eq";
        public static readonly MondValue Gt = "__gt";
        public static readonly MondValue Gte = "__gte";
        public static readonly MondValue Hash = "__hash";
        public static readonly MondValue In = "__in";
        public static readonly MondValue Lshift = "__lshift";
        public static readonly MondValue Lt = "__lt";
        public static readonly MondValue Lte = "__lte";
        public static readonly MondValue Mod = "__mod";
        public static readonly MondValue Mul = "__mul";
        public static readonly MondValue Neg = "__neg";
        public static readonly MondValue Neq = "__neq";
        public static readonly MondValue Not = "__not";
        public static readonly MondValue Number = "__number";
        public static readonly MondValue Or = "__or";
        public static readonly MondValue Pow = "__pow";
        public static readonly MondValue Rshift = "__rshift";
        public static readonly MondValue Serialize = "__serialize";
        public static readonly MondValue Slice = "__slice";
        public static readonly MondValue String = "__string";
        public static readonly MondValue Sub = "__sub";
        public static readonly MondValue Xor = "__xor";
    }
}
