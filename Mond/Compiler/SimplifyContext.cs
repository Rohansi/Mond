using System;

namespace Mond.Compiler
{
    internal class SimplifyContext
    {
        public ExpressionCompiler Compiler { get; }

        public Scope Scope { get; protected set; }
        
        private int FrameDepth => Scope?.FrameDepth ?? 0;
        private int LexicalDepth => Scope?.LexicalDepth ?? 0;

        public bool MakeDeclarationsGlobal => FrameDepth == 0 && LexicalDepth == 0 && Compiler.Options.MakeRootDeclarationsGlobal;

        public SimplifyContext(ExpressionCompiler compiler, Scope prevScope)
        {
            Compiler = compiler;
            Scope = prevScope;
        }

        public Scope PushScope() => PushScopeImpl(0);

        public Scope PushFunctionScope() => PushScopeImpl(1);

        private Scope PushScopeImpl(int frameDepthOffset)
        {
            Compiler.ScopeDepth++;

            var scopeId = Compiler.ScopeId++;
            Scope = new Scope(scopeId, FrameDepth + frameDepthOffset, LexicalDepth + 1, Scope);
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

        public IdentifierOperand Identifier(string name)
        {
            return Scope.Get(name);
        }

        public void ReferenceIdentifier(IdentifierOperand ident)
        {
#if DEBUG
            // sanity check that the ident's scope is actually within the scope chain
            var found = false;
            var scope = Scope;
            while (scope != null)
            {
                if (ident.Scope == scope)
                {
                    found = true;
                    break;
                }

                scope = scope.Previous;
            }

            if (!found)
            {
                throw new InvalidOperationException("Referencing an identifier from an inaccessible scope!");
            }
#endif

            if (ident.Scope.FrameDepth == Scope.FrameDepth)
            {
                // within the same frame so we don't need to track it as captured by a closure
                return;
            }

            ident.IsCaptured = true;
        }
    }
}
