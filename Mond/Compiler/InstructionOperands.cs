using System;
using System.IO;

namespace Mond.Compiler
{
    interface IInstructionOperand
    {
        void Print();

        int Length { get; }
        void Write(BinaryWriter writer);
    }

    class ImmediateOperand : IInstructionOperand
    {
        public readonly int Value;

        public ImmediateOperand(int value)
        {
            Value = value;
        }

        public void Print()
        {
            Console.Write("{0,-30} (immediate)", Value);
        }

        public int Length { get { return 4; } }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Value);
        }
    }

    class ConstantOperand<T> : IInstructionOperand
    {
        public readonly int Id;
        public readonly T Value;

        public ConstantOperand(int id, T value)
        {
            Id = id;
            Value = value;
        }

        public void Print()
        {
            Console.Write("{0,-30} (const {1})", Value, Id);
        }

        public int Length { get { return 4; } }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Id);
        }
    }

    class IdentifierOperand : IInstructionOperand
    {
        public readonly int FrameIndex;
        public readonly int Id;
        public readonly string Name;
        public readonly bool IsReadOnly;

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

        public int Length { get { return 8; } }

        public void Write(BinaryWriter writer)
        {
            writer.Write(FrameIndex);
            writer.Write(Id);
        }
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
        public readonly int Id;
        public readonly string Name;
        public int? Position;

        public LabelOperand(int id, string name = null)
        {
            Id = id;
            Name = name;
            Position = null;
        }

        public void Print()
        {
            var name = string.Format("lbl_{0}{1}{2}", Id, Name != null ? "_" : "", Name);
            Console.Write("{0,-30} (label)", name);
        }

        public int Length { get { return 4; } }

        public void Write(BinaryWriter writer)
        {
            if (!Position.HasValue)
                throw new Exception(string.Format("Label '{0}' not bound", Name));

            writer.Write(Position.Value);
        }
    }
}
