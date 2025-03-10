using System;
using System.Collections.Generic;

namespace Mond.VirtualMachine
{
    internal class ReturnAddress
    {
        public MondProgram Program;
        public int Address;

        public readonly List<MondValue> Arguments = new(16);

        public Closure Closure;
        public int EvalDepth;

        public void Initialize(MondProgram program, int address, Closure closure, int evalDepth)
        {
            Program = program;
            Address = address;
            Arguments.Clear();
            Closure = closure;
            EvalDepth = evalDepth;
        }

        public MondValue GetArgument(int index)
        {
            return index >= 0 && index < Arguments.Count
                ? Arguments[index]
                : MondValue.Undefined;
        }

        public void SetArgument(int index, in MondValue value)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            while (index >= Arguments.Count)
            {
                Arguments.Add(MondValue.Undefined);
            }

            Arguments[index] = value;
        }

        public void ResizeArguments(int newCount)
        {
            var currentCount = Arguments.Count;
            if (newCount == currentCount)
            {
                return;
            }

            if (newCount == 0)
            {
                Arguments.Clear();
                return;
            }

            if (currentCount > newCount)
            {
                Arguments.RemoveRange(newCount, currentCount - newCount);
                return;
            }

            while (currentCount < newCount)
            {
                Arguments.Add(MondValue.Undefined);
                currentCount++;
            }
        }

        public void SetupVarArgs(int fixedArgCount)
        {
            var varArgs = MondValue.Array();

            for (var i = fixedArgCount; i < Arguments.Count; i++)
            {
                varArgs.ArrayValue.Add(Arguments[i]);
            }

            Arguments[fixedArgCount] = varArgs;
            ResizeArguments(fixedArgCount + 1);
        }
    }
}
