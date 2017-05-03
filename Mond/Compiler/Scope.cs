using System;
using System.Collections.Generic;

namespace Mond.Compiler
{
    class Scope
    {
        private readonly Dictionary<string, IdentifierOperand> _identifiers;
        private readonly int _argIndex;
        private readonly int _localIndex;
        private int _nextId;

        public int Id { get; }
        public Scope Previous { get; }
        public Action PopAction { get; }

        public Scope(int id, int argIndex, int localIndex, Scope previous, Action popAction = null)
        {
            _identifiers = new Dictionary<string, IdentifierOperand>();
            _argIndex = argIndex;
            _localIndex = localIndex;
            _nextId = 0;

            Id = id;
            Previous = previous;
            PopAction = popAction;
        }

        public IEnumerable<IdentifierOperand> Identifiers
        {
            get { return _identifiers.Values; }
        } 

        public bool Define(string name, bool isReadOnly)
        {
            if (IsDefined(name))
                return false;

            var frameScope = GetFrameScope();
            var id = frameScope._nextId++;
            var identifier = new IdentifierOperand(_localIndex, id, name, isReadOnly);
            _identifiers.Add(name, identifier);
            return true;
        }

        public bool DefineArgument(int index, string name)
        {
            if (name[0] != '#' && IsDefined(name))
                return false;

            var identifier = new ArgumentIdentifierOperand(-_argIndex, index, name);
            _identifiers.Add(name, identifier);
            return true;
        }

        public IdentifierOperand DefineInternal(string name, bool canHaveMultiple = false)
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

        public IdentifierOperand Get(string name, bool inherit = true)
        {
            if (_identifiers.TryGetValue(name, out var identifier))
                return identifier;

            if (inherit && Previous != null)
                return Previous.Get(name);

            return null;
        }

        public bool IsDefined(string name, bool inherit = true)
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
