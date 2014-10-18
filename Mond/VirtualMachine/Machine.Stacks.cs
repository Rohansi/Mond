using System.Runtime.CompilerServices;

namespace Mond.VirtualMachine
{
    partial class Machine
    {
        private const int CallStackCapacity = 250;
        private const int EvalStackCapacity = 250;

        private readonly ReturnAddress[] _callStack;
        private int _callStackSize;

        private readonly Frame[] _localStack;
        private int _localStackSize;

        private readonly MondValue[] _evalStack;
        private int _evalStackSize;

        public Machine()
        {
            _callStack = new ReturnAddress[CallStackCapacity];
            _callStackSize = 0;

            _localStack = new Frame[CallStackCapacity];
            _localStackSize = 0;

            _evalStack = new MondValue[EvalStackCapacity];
            _evalStackSize = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushCall(ReturnAddress value)
        {
            if (_callStackSize >= CallStackCapacity)
                throw new MondRuntimeException(RuntimeError.StackOverflow);

            _callStack[_callStackSize++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReturnAddress PopCall()
        {
            if (_callStackSize <= 0)
                throw new MondRuntimeException(RuntimeError.StackEmpty);

            var value = _callStack[--_callStackSize];
            _callStack[_callStackSize] = default(ReturnAddress);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReturnAddress PeekCall()
        {
            if (_callStackSize <= 0)
                throw new MondRuntimeException(RuntimeError.StackEmpty);

            return _callStack[_callStackSize - 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushLocal(Frame value)
        {
            if (_localStackSize >= CallStackCapacity)
                throw new MondRuntimeException(RuntimeError.StackOverflow);

            _localStack[_localStackSize++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Frame PopLocal()
        {
            if (_localStackSize <= 0)
                throw new MondRuntimeException(RuntimeError.StackEmpty);

            var value = _localStack[--_localStackSize];
            _localStack[_localStackSize] = default(Frame);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Frame PeekLocal()
        {
            if (_localStackSize <= 0)
                throw new MondRuntimeException(RuntimeError.StackEmpty);

            return _localStack[_localStackSize - 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Push(MondValue value)
        {
            if (_evalStackSize >= EvalStackCapacity)
                throw new MondRuntimeException(RuntimeError.StackOverflow);

            _evalStack[_evalStackSize++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MondValue Pop()
        {
            if (_evalStackSize <= 0)
                throw new MondRuntimeException(RuntimeError.StackEmpty);

            var value = _evalStack[--_evalStackSize];
            _evalStack[_evalStackSize] = default(MondValue);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MondValue Peek()
        {
            if (_evalStackSize <= 0)
                throw new MondRuntimeException(RuntimeError.StackEmpty);

            return _evalStack[_evalStackSize - 1];
        }
    }
}
