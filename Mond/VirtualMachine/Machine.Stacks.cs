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
            _callStackSize = -1;

            _localStack = new Frame[CallStackCapacity];
            _localStackSize = -1;

            _evalStack = new MondValue[EvalStackCapacity];
            _evalStackSize = -1;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushCall(in ReturnAddress value)
        {
            _callStack[++_callStackSize] = value;
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
        private void PushLocal(Frame value)
        {
            _localStack[++_localStackSize] = value;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Frame PopLocal()
        {
            return _localStack[_localStackSize--];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Frame PeekLocal()
        {
            return _localStack[_localStackSize];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref MondValue Reserve()
        {
            return ref _evalStack[++_evalStackSize];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Release()
        {
            _evalStack[_evalStackSize--] = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Release(int n)
        {
            for (var i = 0; i < n; i++)
            {
                Release();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Push(in MondValue value)
        {
            Reserve() = value;
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
        private ref MondValue Pop()
        {
            return ref _evalStack[_evalStackSize--];
        }
    }
}
