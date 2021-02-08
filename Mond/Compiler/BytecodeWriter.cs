using System;

namespace Mond.Compiler
{
    class BytecodeWriter
    {
        private readonly int[] _bytecode;

        public int Offset { get; private set; }

        public BytecodeWriter(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            _bytecode = new int[length];
            Offset = 0;
        }

        public int[] GetBuffer()
        {
            if (Offset != _bytecode.Length)
            {
                throw new InvalidOperationException("Not finished writing bytecode yet.");
            }

            return _bytecode;
        }

        public void Write(int value)
        {
            if (Offset >= _bytecode.Length)
            {
                throw new InvalidOperationException("Attempted to write too much bytecode data.");
            }

            _bytecode[Offset++] = value;
        }

        public void Write(int? value)
        {
            if (value.HasValue)
            {
                Write(value.Value);
            }
        }

        public void Write(InstructionType instructionType, int value)
        {
            if (value > 0x00FFFFFF || value < unchecked((int)0xFF000000))
            {
                throw new ArgumentException("Operand would be overwritten by instruction type.", nameof(value));
            }

            var constructedValue = ((int)instructionType << 24) | (value & 0x00FFFFFF);
            Write(constructedValue);
        }
    }
}
