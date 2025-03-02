using System;
using System.Collections.Generic;

namespace Mond.Compiler
{
    internal class Scope
    {
        private readonly Dictionary<string, IdentifierOperand> _identifiers;
        private int _nextId;

        public int Id { get; }
        public int Depth { get; }
        public Scope Previous { get; }
        public Action PopAction { get; set; }

        private int _identifierCount;
        public int IdentifierCount => GetFrameScope()._identifierCount;

        public Scope(int id, int depth, Scope previous, Action popAction = null)
        {
            _identifiers = new Dictionary<string, IdentifierOperand>();
            _nextId = 0;

            Id = id;
            Depth = depth;
            Previous = previous;
            PopAction = popAction;
        }

        public IEnumerable<IdentifierOperand> Identifiers => _identifiers.Values;

        public bool Define(string name, bool isReadOnly)
        {
            if (IsDefined(name))
                return false;

            var frameScope = GetFrameScope();
            frameScope._identifierCount++;

            var id = frameScope._nextId++;
            var identifier = new IdentifierOperand(Depth, id, name, isReadOnly);
            _identifiers.Add(name, identifier);
            return true;
        }

        public bool DefineArgument(int index, string name)
        {
            if (name[0] != '#' && IsDefined(name))
                return false;

            var identifier = new ArgumentIdentifierOperand(-Depth, index, name);
            _identifiers.Add(name, identifier);
            return true;
        }

        public IdentifierOperand DefineInternal(string name, bool canHaveMultiple = false)
        {
            name = "#" + name;

            var frameScope = GetFrameScope();
            frameScope._identifierCount++;

            var id = frameScope._nextId++;

            IdentifierOperand identifier;

            if (canHaveMultiple)
            {
                var n = 0;
                string numberedName;

                while (true)
                {
                    numberedName = $"{name}_{n++}";

                    if (!IsDefined(numberedName))
                        break;
                }

                identifier = new IdentifierOperand(Depth, id, numberedName, false);
                _identifiers.Add(numberedName, identifier);
                return identifier;
            }

            identifier = new IdentifierOperand(Depth, id, name, false);
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
                while (curr.Depth == Depth)
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
