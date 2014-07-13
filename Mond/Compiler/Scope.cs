using System.Collections.Generic;

namespace Mond.Compiler
{
    class Scope
    {
        private readonly Dictionary<string, IdentifierOperand> _identifiers;
        private readonly int _frameIndex;
        private int _nextId;

        public readonly Scope Previous;

        public Scope(int frameIndex, Scope previous)
        {
            _identifiers = new Dictionary<string, IdentifierOperand>();
            _frameIndex = frameIndex;
            _nextId = 0;

            Previous = previous;
        }

        public bool Define(string name)
        {
            if (IsDefined(name))
                return false;

            Scope frameScope = null;

            if (Previous != null)
            {
                var curr = Previous;
                while (curr._frameIndex == _frameIndex)
                {
                    frameScope = curr;
                    
                    if (curr.Previous == null)
                        break;

                    curr = curr.Previous;
                }
            }

            var id = frameScope != null ? frameScope._nextId++ : _nextId++;

            var identifier = new IdentifierOperand(_frameIndex, id, name);
            _identifiers.Add(name, identifier);
            return true;
        }

        public bool DefineArgument(int index, string name)
        {
            if (IsDefined(name))
                return false;

            var identifier = new ArgumentIdentifierOperand(-_frameIndex, index, name);
            _identifiers.Add(name, identifier);
            return true;
        }

        public IdentifierOperand Get(string name, bool inherit = true)
        {
            IdentifierOperand identifier;
            if (_identifiers.TryGetValue(name, out identifier))
                return identifier;

            if (inherit && Previous != null)
                return Previous.Get(name);

            return null;
        }

        private bool IsDefined(string name, bool inherit = true)
        {
            return Get(name, inherit) != null;
        }
    }
}
