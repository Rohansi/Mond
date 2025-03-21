using System;
using System.Collections.Generic;
using System.Linq;

namespace Mond.Compiler
{
    internal class Scope
    {
        private readonly Dictionary<string, IdentifierOperand> _identifiers;
        private bool _finishedPreprocess;
        private int _nextId;

        public int Id { get; }
        public int FrameDepth { get; }
        public int LexicalDepth { get; }
        public Scope Previous { get; }
        public Action PopAction { get; set; }
        public IdentifierOperand CaptureArray { get; private set; }

        public int IdentifierCount => _nextId;

        public IEnumerable<IdentifierOperand> Identifiers => _identifiers.Values;

        public Scope(int id, int frameDepth, int lexicalDepth, Scope previous, Action popAction = null)
        {
            _identifiers = new Dictionary<string, IdentifierOperand>();
            _nextId = 0;

            Id = id;
            FrameDepth = frameDepth;
            LexicalDepth = lexicalDepth;
            Previous = previous;
            PopAction = popAction;
        }

        public bool Define(string name, bool isReadOnly)
        {
            if (_finishedPreprocess)
                throw new InvalidOperationException();

            if (IsDefined(name))
                return false;

            var frameScope = GetFrameScope();
            var identifier = new IdentifierOperand(this, FrameDepth, name, isReadOnly);
            _identifiers.Add(name, identifier);
            return true;
        }

        public bool DefineArgument(int index, string name)
        {
            if (_finishedPreprocess)
                throw new InvalidOperationException();

            if (name[0] != '#' && IsDefined(name))
                return false;

            var identifier = new ArgumentIdentifierOperand(this, FrameDepth, index, name);
            identifier.Id = index;
            _identifiers.Add(name, identifier);
            return true;
        }

        public IdentifierOperand DefineInternal(string name, bool canHaveMultiple = false)
        {
            name = "#" + name;

            var frameScope = GetFrameScope();

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

                identifier = new IdentifierOperand(this, FrameDepth, numberedName, false);
            }
            else
            {
                if (IsDefined(name))
                {
                    throw new InvalidOperationException($"Cannot define multiple internal variables named `{name}`");
                }

                identifier = new IdentifierOperand(this, FrameDepth, name, false);
            }

            _identifiers.Add(identifier.Name, identifier);

            // we only support adding new internal variables after the first pass. these are never allowed to be captured!
            if (_finishedPreprocess)
            {
                identifier.Id = frameScope._nextId++;
            }

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

        public (IdentifierOperand CaptureArray, int CaptureCount) Preprocess()
        {
            if (_finishedPreprocess)
            {
                throw new InvalidOperationException();
            }
            
            var frameScope = GetFrameScope();
            var hasCapturedVars = _identifiers.Values.Any(i => i.IsCaptured);
            var captureArray = hasCapturedVars
                ? DefineInternal($"frame_{Id}")
                : null;
            var nextCaptureId = 0;

            foreach (var identifier in _identifiers.Values)
            {
                if (identifier.IsCaptured)
                {
                    identifier.Id = nextCaptureId++;
                    identifier.CaptureArray = captureArray;
                }
                else if (identifier is not ArgumentIdentifierOperand)
                {
                    identifier.Id = frameScope._nextId++;
                }
            }

            _finishedPreprocess = true;
            CaptureArray = captureArray;
            return (captureArray, nextCaptureId);
        }

        private Scope GetFrameScope()
        {
            Scope frameScope = null;

            if (Previous != null)
            {
                var curr = Previous;
                while (curr.FrameDepth == FrameDepth)
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
