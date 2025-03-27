using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mond.Compiler
{
    interface IInstructionOperand
    {
        void Print();

        /// <summary>
        /// Size in ints (so length 1 is 4 bytes)
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Value to encode with the instruction type (24 bits max)
        /// </summary>
        int? FirstValue { get; }

        /// <summary>
        /// Writes additional data (not including FirstValue!)
        /// </summary>
        void Write(BytecodeWriter writer);
    }

    class DeferredOperand<T> : IInstructionOperand where T : IInstructionOperand
    {
        private readonly Func<T> _valueFactory;
        private bool _hasValue;
        private T _value;

        public T Value
        {
            get
            {
                if (!_hasValue)
                {
                    _value = _valueFactory();
                    _hasValue = true;
                }

                return _value;
            }
        }

        public DeferredOperand(Func<T> valueFactory)
        {
            _valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
            _hasValue = false;
            _value = default;
        }

        public void Print()
        {
            Value.Print();
        }

        public int Length => Value.Length;

        public int? FirstValue => Value.FirstValue;

        public void Write(BytecodeWriter writer)
        {
            Value.Write(writer);
        }
    }

    class ListOperand<T> : IInstructionOperand where T : IInstructionOperand
    {
        public ReadOnlyCollection<T> Operands { get; }

        public ListOperand(List<T> operands)
        {
            Operands = operands.AsReadOnly();
        }

        public void Print()
        {
            foreach (var operand in Operands)
            {
                operand.Print();
            }
        }

        public int Length => Operands.Sum(o => o.Length);

        public int? FirstValue => Operands.Count > 0
            ? Operands[0].FirstValue
            : null;

        public void Write(BytecodeWriter writer)
        {
            if (Operands.Count == 0)
            {
                return;
            }

            Operands[0].Write(writer);

            foreach (var operand in Operands.Skip(1))
            {
                writer.Write(operand.FirstValue);
                operand.Write(writer);
            }
        }
    }

    class ImmediateOperand : IInstructionOperand
    {
        public int Value { get; }

        public ImmediateOperand(int value)
        {
            Value = value;
        }

        public void Print()
        {
            Console.Write("{0,-30} (immediate)", Value);
        }

        public int Length => 1;

        public int? FirstValue => Value;

        public void Write(BytecodeWriter writer) { }
    }

    class ConstantOperand<T> : IInstructionOperand
    {
        public int Id { get; }
        public T Value { get; }

        public ConstantOperand(int id, T value)
        {
            Id = id;
            Value = value;
        }

        public void Print()
        {
            Console.Write("{0,-30} (const {1})", Value, Id);
        }

        public int Length => 1;

        public int? FirstValue => Id;

        public void Write(BytecodeWriter writer) { }
    }

    class IdentifierOperand : IInstructionOperand
    {
        public Scope Scope { get; }
        public int FrameIndex { get; }
        public int Id { get; set; }
        public string Name { get; }
        public bool IsReadOnly { get; }
        public bool IsGlobal { get; }
        public bool IsCaptured { get; set; }
        public IdentifierOperand CaptureArray { get; set; }

        public IdentifierOperand(Scope scope, int frameIndex, string name, bool isReadOnly, bool isGlobal)
        {
            Scope = scope;
            FrameIndex = frameIndex;
            Name = name;
            IsReadOnly = isReadOnly;
            IsGlobal = isGlobal;
        }

        public virtual void Print()
        {
            if (IsGlobal)
            {
                Console.WriteLine("{0,-30} (global)", Name);
            }
            if (IsCaptured)
            {
                Console.Write("{0,-30} (frame {1} capture {2} array {3})", Name, FrameIndex, Id, CaptureArray.Id);
            }
            else
            {
                Console.Write("{0,-30} (frame {1} ident {2})", Name, FrameIndex, Id);
            }
        }

        public int Length => 2;

        public int? FirstValue => FrameIndex;

        public void Write(BytecodeWriter writer) => writer.Write(Id);
    }

    class ArgumentIdentifierOperand : IdentifierOperand
    {
        public int ArgumentId { get; }

        public ArgumentIdentifierOperand(Scope scope, int frameIndex, int index, string name)
            : base(scope, frameIndex, name, false, false)
        {
            ArgumentId = index;
        }

        public override void Print()
        {
            Console.Write("{0,-30} (frame {1} arg {2})", Name, FrameIndex, Id);
        }
    }

    class LabelOperand : IInstructionOperand
    {
        public int Id { get; }
        public string Name { get; }
        public int? Position;

        public LabelOperand(int id, string name = null)
        {
            Id = id;
            Name = name;
            Position = null;
        }

        public void Print()
        {
            var name = $"lbl_{Id}{(Name != null ? "_" : "")}{Name}";
            Console.Write("{0,-30} (label)", name);
        }

        public int Length => 1;

        public int? FirstValue
        {
            get
            {
                if (!Position.HasValue)
                    throw new Exception($"Label '{Name}' not bound");

                return Position.Value;
            }
        }

        public void Write(BytecodeWriter writer) { }
    }

    class DebugIdentifierOperand : IInstructionOperand
    {
        public ConstantOperand<string> Name { get; }
        public bool IsReadOnly { get; }
        public bool IsGlobal { get; }
        public bool IsCaptured { get; }
        public bool IsArgument { get; }
        public int FrameIndex { get; }
        public int Id { get; }

        public DebugIdentifierOperand(ConstantOperand<string> name, bool isReadOnly, bool isGlobal, bool isCaptured, bool isArgument, int frameIndex, int id)
        {
            Name = name;
            IsReadOnly = isReadOnly;
            IsGlobal = isGlobal;
            IsCaptured = isCaptured;
            IsArgument = isArgument;
            FrameIndex = frameIndex;
            Id = id;
        }

        public virtual void Print()
        {
            var flags = "r";
            if (!IsReadOnly) flags += "w";
            if (IsCaptured) flags += "c";
            if (IsArgument) flags = "a" + flags;
            if (IsGlobal) flags = "g" + flags;
            var desc = $"{FrameIndex}:{Id}={Name.Value}[{flags}]";
            Console.Write("{0,-30} (dbgident)", desc);
        }

        public int Length => 0;

        public int? FirstValue => throw new NotSupportedException();

        public void Write(BytecodeWriter writer) => throw new NotSupportedException();
    }
}
