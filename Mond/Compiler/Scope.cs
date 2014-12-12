using System.Collections.Generic;

namespace Mond.Compiler
{
    class Scope
    {
        private readonly Dictionary<string, IdentifierOperand> _identifiers;
        private readonly int _argIndex;
        private readonly int _localIndex;
        private int _nextId;

        public readonly Scope Previous;

        public Scope(int argIndex, int localIndex, Scope previous)
        {
            _identifiers = new Dictionary<string, IdentifierOperand>();
            _argIndex = argIndex;
            _localIndex = localIndex;
            _nextId = 0;

            Previous = previous;
        }

        public virtual bool Define(string name, bool isReadOnly)
        {
            if (IsDefined(name))
                return false;

            var frameScope = GetFrameScope();
            var id = frameScope._nextId++;
            var identifier = new IdentifierOperand(_localIndex, id, name, isReadOnly);
            _identifiers.Add(name, identifier);
            return true;
        }

        public virtual bool DefineArgument(int index, string name)
        {
            if (name[0] != '#' && IsDefined(name))
                return false;

            var identifier = new ArgumentIdentifierOperand(-_argIndex, index, name);
            _identifiers.Add(name, identifier);
            return true;
        }

        public virtual IdentifierOperand DefineInternal(string name, bool canHaveMultiple = false)
        {
            name = "#" + name;

            var frameScope = GetFrameScope();
            var id = frameScope._nextId++;

            IdentifierOperand identifier;

            if (canHaveMultiple)
            {
                var n = 0;
                string numberedName;

                while (true)
                {
                    numberedName = string.Format("{0}_{1}", name, n++);

                    if (!IsDefined(numberedName))
                        break;
                }

                identifier = new IdentifierOperand(_localIndex, id, numberedName, false);
                _identifiers.Add(numberedName, identifier);
                return identifier;
            }

            identifier = new IdentifierOperand(_localIndex, id, name, false);
            _identifiers.Add(name, identifier);
            return identifier;
        }

        public virtual IdentifierOperand Get(string name, bool inherit = true)
        {
            IdentifierOperand identifier;
            if (_identifiers.TryGetValue(name, out identifier))
                return identifier;

            if (inherit && Previous != null)
                return Previous.Get(name);

            return null;
        }

        protected bool IsDefined(string name, bool inherit = true)
        {
            return Get(name, inherit) != null;
        }

        private Scope GetFrameScope()
        {
            Scope frameScope = null;

            if (Previous != null)
            {
                var curr = Previous;
                while (curr._localIndex == _localIndex)
                {
                    frameScope = curr;

                    if (curr.Previous == null)
                        break;

                    curr = curr.Previous;
                }
            }

            return frameScope ?? this;
        }
    }
}
