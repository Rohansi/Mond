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
        public int FrameIndex { get; }
        public int Id { get; }
        public string Name { get; }
        public bool IsReadOnly { get; }

        public IdentifierOperand(int frameIndex, int id, string name, bool isReadOnly)
        {
            FrameIndex = frameIndex;
            Id = id;
            Name = name;
            IsReadOnly = isReadOnly;
        }

        public virtual void Print()
        {
            Console.Write("{0,-30} (frame {1} ident {2})", Name, FrameIndex, Id);
        }

        public int Length => 2;

        public int? FirstValue => FrameIndex;

        public void Write(BytecodeWriter writer) => writer.Write(Id);
    }

    class ArgumentIdentifierOperand : IdentifierOperand
    {
        public ArgumentIdentifierOperand(int frameIndex, int id, string name)
            : base(frameIndex, id, name, false)
        {
            
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
        public int FrameIndex { get; }
        public int Id { get; }

        public DebugIdentifierOperand(ConstantOperand<string> name, bool isReadOnly, int frameIndex, int id)
        {
            Name = name;
            IsReadOnly = isReadOnly;
            FrameIndex = frameIndex;
            Id = id;
        }

        public virtual void Print()
        {
            var desc = $"{FrameIndex}:{Id}={Name.Value}[{(IsReadOnly ? "r" : "rw")}]";
            Console.Write("{0,-30} (dbgident)", desc);
        }

        public int Length => 0;

        public int? FirstValue => throw new NotSupportedException();

        public void Write(BytecodeWriter writer) => throw new NotSupportedException();
    }
}
