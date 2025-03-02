namespace Mond.Compiler
{
    internal class SimplifyContext
    {
        public ExpressionCompiler Compiler { get; }

        public Scope Scope { get; protected set; }
        
        private int Depth => Scope?.Depth ?? 0;
        public bool MakeDeclarationsGlobal => Depth == 0 && Compiler.Options.MakeRootDeclarationsGlobal;

        public SimplifyContext(ExpressionCompiler compiler, Scope prevScope)
        {
            Compiler = compiler;
            Scope = prevScope;
        }

        public Scope PushScope() => PushScopeImpl(0);

        public Scope PushFunctionScope() => PushScopeImpl(1);

        private Scope PushScopeImpl(int depthOffset)
        {
            Compiler.ScopeDepth++;

            var scopeId = Compiler.ScopeId++;
            Scope = new Scope(scopeId, Depth + depthOffset, Scope);
            return Scope;
        }

        public void PopScope()
        {
            Scope.PopAction?.Invoke();

            Scope = Scope.Previous;
            Compiler.ScopeDepth--;
        }

        public bool DefineIdentifier(string name, bool isReadOnly = false)
        {
            return Scope.Define(name, isReadOnly);
        }

        public IdentifierOperand DefineInternal(string name, bool canHaveMultiple = false)
        {
            return Scope.DefineInternal(name, canHaveMultiple);
        }
    }
}
