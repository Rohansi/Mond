using System;
using System.Runtime.CompilerServices;

namespace Mond.VirtualMachine
{
    partial class Machine
    {
        private const int CallStackCapacity = 250;
        private const int EvalStackCapacity = 250;

        private readonly ReturnAddress[] _callStack;
        private int _callStackSize;

        private readonly MondValue[][] _localStack;
        private int _localStackSize;

        private readonly MondValue[] _evalStack;
        private int _evalStackSize;
        private int _evalStackDirtySize;

        public Machine()
        {
            _callStack = new ReturnAddress[CallStackCapacity];
            _callStackSize = -1;

            _localStack = new MondValue[CallStackCapacity][];
            _localStackSize = -1;

            _evalStack = new MondValue[EvalStackCapacity];
            _evalStackSize = -1;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReturnAddress PushCall()
        {
            return _callStack[++_callStackSize] ??= new ReturnAddress();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReturnAddress PopCall()
        {
            return _callStack[_callStackSize--];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReturnAddress PeekCall()
        {
            return _callStack[_callStackSize];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushLocal(MondValue[] value)
        {
            _localStack[++_localStackSize] = value;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PopLocal(bool returnToPool)
        {
            var locals = _localStack[_localStackSize--];
            if (returnToPool)
            {
                _arrayPool.Return(locals);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MondValue[] PeekLocal()
        {
            return _localStack[_localStackSize];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Push(in MondValue value)
        {
            _evalStack[++_evalStackSize] = value;
            if (_evalStackSize > _evalStackDirtySize)
            {
                _evalStackDirtySize = _evalStackSize;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref MondValue Peek()
        {
            return ref _evalStack[_evalStackSize];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref MondValue Peek(int n)
        {
            return ref _evalStack[_evalStackSize - n];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref readonly MondValue Pop()
        {
            return ref _evalStack[_evalStackSize--];
        }

        private void ClearDirty()
        {
            for (var i = _evalStackSize + 1; i <= _evalStackDirtySize; i++)
            {
                _evalStack[i] = default;
            }
        }
    }
}
