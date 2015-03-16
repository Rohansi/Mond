#if !UNITY
using System.Runtime.CompilerServices;
#endif

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
        
#if !UNITY
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void PushCall(ReturnAddress value)
        {
            _callStack[_callStackSize++] = value;
        }
        
#if !UNITY
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private ReturnAddress PopCall()
        {
            var value = _callStack[--_callStackSize];
            _callStack[_callStackSize] = default(ReturnAddress);
            return value;
        }
        
#if !UNITY
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private ReturnAddress PeekCall()
        {
            if (_callStackSize == 0)
                throw new MondRuntimeException(RuntimeError.StackEmpty);

            return _callStack[_callStackSize - 1];
        }
        
#if !UNITY
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void PushLocal(Frame value)
        {
            _localStack[_localStackSize++] = value;
        }
        
#if !UNITY
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private Frame PopLocal()
        {
            var value = _localStack[--_localStackSize];
            _localStack[_localStackSize] = default(Frame);
            return value;
        }
        
#if !UNITY
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private Frame PeekLocal()
        {
            if (_localStackSize == 0)
                throw new MondRuntimeException(RuntimeError.StackEmpty);

            return _localStack[_localStackSize - 1];
        }
        
#if !UNITY
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void Push(MondValue value)
        {
            _evalStack[_evalStackSize++] = value;
        }
        
#if !UNITY
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private MondValue Pop()
        {
            var value = _evalStack[--_evalStackSize];
            _evalStack[_evalStackSize] = default(MondValue);
            return value;
        }
        
#if !UNITY
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private MondValue Peek()
        {
            if (_evalStackSize == 0)
                throw new MondRuntimeException(RuntimeError.StackEmpty);

            return _evalStack[_evalStackSize - 1];
        }
    }
}
