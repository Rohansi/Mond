using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Mond.Compiler;
using Mond.Debugger;

namespace Mond.VirtualMachine
{
    internal partial class Machine
    {
        private readonly MondState _state;
        private readonly ArrayPool<MondValue> _arrayPool = new(32, 16);

        internal MondValue Global;

#if !NO_DEBUG
        private MondDebugAction _debugAction;
        private int _debugDepth;
        internal MondDebugger Debugger;
#endif

        public Machine(MondState state)
            : this()
        {
            _state = state;
            Global = MondValue.Object(state);

#if !NO_DEBUG
            _debugAction = MondDebugAction.Run;
            _debugDepth = 0;
            Debugger = null;
#endif
        }

        public string CurrentScript
        {
            get
            {
                if (_callStackSize == -1)
                    throw new InvalidOperationException("No scripts are running");

                return _callStack[_callStackSize].Program.DebugInfo?.FileName;
            }
            
        }

        public MondValue Load(MondProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));

            return Call(program.EntryPoint);
        }

        public MondValue Call(MondValue function, params Span<MondValue> arguments)
        {
            if (function.Type == MondValueType.Object)
            {
                // insert "this" value into argument array
                using var argsCopyHandle = _arrayPool.Rent(arguments.Length + 1);
                var argsCopy = argsCopyHandle.Span;
                arguments.CopyTo(argsCopy[1..]);
                argsCopy[0] = function;

                if (function.TryDispatch("__call", out var result, argsCopy))
                    return result;
            }

            if (function.Type != MondValueType.Function)
                throw new MondRuntimeException(RuntimeError.ValueNotCallable, function.Type.GetName());

            var closure = function.FunctionValue;

            switch (closure.Type)
            {
                case ClosureType.Mond:
                    var returnAddress = PushCall();
                    returnAddress.Initialize(closure.Program, closure.Address, closure, (short)_evalStackSize, true);
                    foreach (var arg in arguments)
                    {
                        returnAddress.Arguments.Add(arg);
                    }

                    DebuggerOnCall();
                    break;

                case ClosureType.Native:
                    return closure.NativeFunction(_state, arguments);

                default:
                    throw new NotSupportedException();
            }

            return Run();
        }

        private MondValue Run()
        {
            var functionAddress = PeekCall();
            var program = functionAddress.Program;
            var code = program.Bytecode;

            var initialCallDepth = _callStackSize - 1; // "- 1" to not include values pushed by Call()
            var initialLocalDepth = _localStackSize;
            var initialEvalDepth = _evalStackSize;
            var initialEvalDirtyDepth = _evalStackDirtySize;

            // to allow re-entrancy with our ref stack set up we need to preserve the dirty portion of the stack
            // this is because we may have Pop()'d a value to use but then had to run more code (ex. metamethod)
            _evalStackSize = initialEvalDirtyDepth;
            _evalStackDirtySize = initialEvalDirtyDepth;

            var ip = functionAddress.Address;
            var errorIp = 0;

            MondValue[] locals = null;

            try
            {
                while (true)
                {
                    errorIp = ip;

                    var opcode = code[ip++];
                    var instruction = opcode >> 24;

                    //Console.WriteLine($"{ip - 1:X4} {(InstructionType)instruction}");
                    switch (instruction)
                    {
                        #region Stack Manipulation
                        case (int)InstructionType.Dup:
                            {
                                Push(Peek());
                                break;
                            }

                        case (int)InstructionType.Dup2:
                            {
                                ref readonly var value2 = ref Peek();
                                ref readonly var value1 = ref Peek(1);
                                Push(value1);
                                Push(value2);
                                break;
                            }

                        case (int)InstructionType.Drop:
                            {
                                Pop();
                                break;
                            }

                        case (int)InstructionType.Swap:
                            {
                                ref var value1 = ref Peek();
                                ref var value2 = ref Peek(1);
                                var temp = value1;
                                value1 = value2;
                                value2 = temp;
                                break;
                            }

                        case (int)InstructionType.Swap1For2:
                            {
                                ref var one = ref Peek();
                                ref var two2 = ref Peek(1);
                                ref var two1 = ref Peek(2);
                                var temp = one;
                                one = two2;
                                two2 = two1;
                                two1 = temp;
                                break;
                            }
                        #endregion

                        #region Constants
                        case (int)InstructionType.LdUndef:
                            {
                                Push(MondValue.Undefined);
                                break;
                            }

                        case (int)InstructionType.LdNull:
                            {
                                Push(MondValue.Null);
                                break;
                            }

                        case (int)InstructionType.LdTrue:
                            {
                                Push(MondValue.True);
                                break;
                            }

                        case (int)InstructionType.LdFalse:
                            {
                                Push(MondValue.False);
                                break;
                            }

                        case (int)InstructionType.LdNum:
                            {
                                var index = UnpackFirstOperand(opcode);
                                Push(program.Numbers[index]);
                                break;
                            }

                        case (int)InstructionType.LdStr:
                            {
                                var index = UnpackFirstOperand(opcode);
                                Push(program.Strings[index]);
                                break;
                            }

                        case (int)InstructionType.LdGlobal:
                            {
                                Push(Global);
                                break;
                            }
                        #endregion

                        #region Storables
                        case (int)InstructionType.LdLocF:
                            {
                                var index = UnpackFirstOperand(opcode);
                                Push(locals[index]);
                                break;
                            }

                        case (int)InstructionType.StLocF:
                            {
                                var index = UnpackFirstOperand(opcode);
                                locals[index] = Pop();
                                break;
                            }

                        case (int)InstructionType.LdArgF:
                            {
                                var index = UnpackFirstOperand(opcode);
                                Push(functionAddress.GetArgument(index));
                                break;
                            }

                        case (int)InstructionType.StArgF:
                            {
                                var index = UnpackFirstOperand(opcode);
                                functionAddress.SetArgument(index, in Pop());
                                break;
                            }

                        case (int)InstructionType.LdFld:
                            {
                                ref var value = ref Peek();
                                var stringIndex = UnpackFirstOperand(opcode);
                                value = value[program.Strings[stringIndex]];
                                break;
                            }

                        case (int)InstructionType.StFld:
                            {
                                ref readonly var obj = ref Pop();
                                ref readonly var value = ref Pop();
                                var stringIndex = UnpackFirstOperand(opcode);
                                obj[program.Strings[stringIndex]] = value;
                                break;
                            }

                        case (int)InstructionType.LdArr:
                            {
                                ref readonly var index = ref Pop();
                                ref var value = ref Peek();
                                value = value[index];
                                break;
                            }

                        case (int)InstructionType.StArr:
                            {
                                ref readonly var index = ref Pop();
                                ref readonly var array = ref Pop();
                                ref readonly var value = ref Pop();
                                array[index] = value;
                                break;
                            }

                        case (int)InstructionType.LdArrF:
                            {
                                var arrayLocal = UnpackFirstOperand(opcode);
                                var arrayIndex = code[ip++];
                                ref readonly var array = ref locals[arrayLocal];
                                Push(array[arrayIndex]);
                                break;
                            }

                        case (int)InstructionType.StArrF:
                            {
                                var arrayLocal = UnpackFirstOperand(opcode);
                                var arrayIndex = code[ip++];
                                ref readonly var array = ref locals[arrayLocal];
                                array[arrayIndex] = Pop();
                                break;
                            }

                        case (int)InstructionType.LdUp:
                            {
                                var index = UnpackFirstOperand(opcode);
                                var value = functionAddress.Closure.Upvalues[index];
                                Push(value);
                                break;
                            }

                        case (int)InstructionType.LdUpValue:
                            {
                                var depth = UnpackFirstOperand(opcode);
                                var index = code[ip++];
                                var value = functionAddress.Closure.Upvalues[depth][index];
                                Push(value);
                                break;
                            }

                        case (int)InstructionType.StUpValue:
                            {
                                var depth = UnpackFirstOperand(opcode);
                                var index = code[ip++];
                                var value = Pop();
                                functionAddress.Closure.Upvalues[depth][index] = value;
                                break;
                            }

                        case (int)InstructionType.SeqResume:
                            {
                                var closure = functionAddress.Closure;
                                if (closure.StoredFrame == null)
                                {
                                    throw new InvalidOperationException();
                                }

                                // sequences have an `enter` instruction before jumping to the resume point so we need to pop that and then replace with the saved locals
                                PopLocal(true);
                                locals = closure.StoredFrame;
                                PushLocal(locals);

                                var evals = closure.StoredEvals;
                                if (evals != null)
                                {
                                    for (var i = evals.Count - 1; i >= 0; i--)
                                    {
                                        Push(evals[i]); // TODO: use array
                                    }

                                    evals.Clear();
                                }

                                break;
                            }

                        case (int)InstructionType.SeqSuspend:
                            {
                                var closure = functionAddress.Closure;
                                closure.StoredFrame = locals;

                                var initialEvals = _callStackSize >= 0 ? PeekCall().EvalDepth : 0;
                                var currentEvals = _evalStackSize;

                                if (currentEvals != initialEvals)
                                {
                                    var evals = closure.StoredEvals ??= new List<MondValue>(); // TODO: use array

                                    while (currentEvals != initialEvals)
                                    {
                                        evals.Add(Pop()); // TODO: use array
                                        currentEvals--;
                                    }
                                }

                                Push(in MondValue.True);

                                if (DoReturn(initialCallDepth, initialEvalDepth, initialEvalDirtyDepth, false, out program,
                                        out code, out ip, out functionAddress, out locals, out var returnValue))
                                {
                                    return returnValue;
                                }

                                break;
                            }
                        #endregion

                        #region Object Creation
                        case (int)InstructionType.NewObject:
                            {
                                var obj = MondValue.Object(_state);
                                Push(obj);
                                break;
                            }

                        case (int)InstructionType.NewArray:
                            {
                                var count = UnpackFirstOperand(opcode);
                                var array = MondValue.Array();
                                array.ArrayValue.Capacity = count;

                                for (var i = 0; i < count; i++)
                                    array.ArrayValue.Add(default);

                                Push(array);
                                break;
                            }

                        case (int)InstructionType.Slice:
                            {
                                ref readonly var step = ref Pop();
                                ref readonly var end = ref Pop();
                                ref readonly var start = ref Pop();
                                ref var array = ref Peek();
                                array = array.Slice(start, end, step);
                                break;
                            }
                        #endregion

                        #region Math
                        case (int)InstructionType.Add:
                            {
                                ref readonly var right = ref Pop();
                                ref var left = ref Peek();
                                left += right;
                                break;
                            }

                        case (int)InstructionType.Sub:
                            {
                                ref readonly var right = ref Pop();
                                ref var left = ref Peek();
                                left -= right;
                                break;
                            }

                        case (int)InstructionType.Mul:
                            {
                                ref readonly var right = ref Pop();
                                ref var left = ref Peek();
                                left *= right;
                                break;
                            }

                        case (int)InstructionType.Div:
                            {
                                ref readonly var right = ref Pop();
                                ref var left = ref Peek();
                                left /= right;
                                break;
                            }

                        case (int)InstructionType.Mod:
                            {
                                ref readonly var right = ref Pop();
                                ref var left = ref Peek();
                                left %= right;
                                break;
                            }

                        case (int)InstructionType.Exp:
                            {
                                ref readonly var right = ref Pop();
                                ref var left = ref Peek();
                                left = left.Pow(right);
                                break;
                            }

                        case (int)InstructionType.BitLShift:
                            {
                                ref readonly var right = ref Pop();
                                ref var left = ref Peek();
                                left = left.LShift(right);
                                break;
                            }

                        case (int)InstructionType.BitRShift:
                            {
                                ref readonly var right = ref Pop();
                                ref var left = ref Peek();
                                left = left.RShift(right);
                                break;
                            }

                        case (int)InstructionType.BitAnd:
                            {
                                ref readonly var right = ref Pop();
                                ref var left = ref Peek();
                                left &= right;
                                break;
                            }

                        case (int)InstructionType.BitOr:
                            {
                                ref readonly var right = ref Pop();
                                ref var left = ref Peek();
                                left |= right;
                                break;
                            }

                        case (int)InstructionType.BitXor:
                            {
                                ref readonly var right = ref Pop();
                                ref var left = ref Peek();
                                left ^= right;
                                break;
                            }

                        case (int)InstructionType.Neg:
                            {
                                ref var value = ref Peek();
                                value = -value;
                                break;
                            }

                        case (int)InstructionType.BitNot:
                            {
                                ref var value = ref Peek();
                                value = ~value;
                                break;
                            }

                        case (int)InstructionType.IncF:
                            {
                                var index = UnpackFirstOperand(opcode);
                                ref var value = ref locals[index];
                                value++;
                                break;
                            }

                        case (int)InstructionType.DecF:
                            {
                                var index = UnpackFirstOperand(opcode);
                                ref var value = ref locals[index];
                                value--;
                                break;
                            }
                        #endregion

                        #region Logic
                        case (int)InstructionType.Eq:
                            {
                                ref readonly var right = ref Pop();
                                ref var left = ref Peek();
                                left = left == right;
                                break;
                            }

                        case (int)InstructionType.Neq:
                            {
                                ref readonly var right = ref Pop();
                                ref var left = ref Peek();
                                left = left != right;
                                break;
                            }

                        case (int)InstructionType.Gt:
                            {
                                ref readonly var right = ref Pop();
                                ref var left = ref Peek();
                                left = left > right;
                                break;
                            }

                        case (int)InstructionType.Gte:
                            {
                                ref readonly var right = ref Pop();
                                ref var left = ref Peek();
                                left = left >= right;
                                break;
                            }

                        case (int)InstructionType.Lt:
                            {
                                ref readonly var right = ref Pop();
                                ref var left = ref Peek();
                                left = left < right;
                                break;
                            }

                        case (int)InstructionType.Lte:
                            {
                                ref readonly var right = ref Pop();
                                ref var left = ref Peek();
                                left = left <= right;
                                break;
                            }

                        case (int)InstructionType.Not:
                            {
                                ref var value = ref Peek();
                                value = !value;
                                break;
                            }

                        case (int)InstructionType.In:
                            {
                                ref readonly var right = ref Pop();
                                ref var left = ref Peek();
                                left = right.Contains(left);
                                break;
                            }

                        case (int)InstructionType.NotIn:
                            {
                                ref readonly var right = ref Pop();
                                ref var left = ref Peek();
                                left = !right.Contains(left);
                                break;
                            }
                        #endregion

                        #region Functions
                        case (int)InstructionType.Closure:
                            {
                                var upvalueCount = UnpackFirstOperand(opcode);
                                var address = code[ip++];

                                var upvalues = upvalueCount > 0
                                    ? new MondValue[upvalueCount]
                                    : Array.Empty<MondValue>();
                                for (var i = 0; i < upvalueCount; i++)
                                {
                                    ref readonly var value = ref Pop();
                                    if (value.Type == MondValueType.Array)
                                    {
                                        upvalues[i] = value;
                                    }
                                }

                                Push(new MondValue(new Closure(program, address, upvalues)));
                                break;
                            }

                        case (int)InstructionType.Call:
                            {
                                DoCall(UnpackFirstOperand(opcode), ref code, ref ip, ref program, ref functionAddress, ref locals);
                                break;
                            }

                        case (int)InstructionType.TailCall:
                            {
                                var argCount = UnpackFirstOperand(opcode);
                                var address = code[ip++];
                                var unpackCount = code[ip++];

                                List<MondValue> unpackedArgs = null;

                                if (unpackCount > 0)
                                    unpackedArgs = UnpackArgs(code, ref ip, argCount, unpackCount);
                                
                                var unpackedArgCount = unpackedArgs?.Count ?? argCount;
                                functionAddress.ResizeArguments(unpackedArgCount);

                                // replace arguments in current frame
                                if (unpackedArgs == null)
                                {
                                    for (var i = unpackedArgCount - 1; i >= 0; i--)
                                    {
                                        functionAddress.Arguments[i] = Pop();
                                    }
                                }
                                else
                                {
                                    for (var i = 0; i < unpackedArgCount; i++)
                                    {
                                        functionAddress.Arguments[i] = unpackedArgs[i];
                                    }
                                }

                                // get rid of old locals, because tailcall is a variant of ret
                                PopLocal(true);

                                functionAddress.EvalDepth = (short)_evalStackSize;
                                ip = address;
                                break;
                            }

                        case (int)InstructionType.InstanceCall:
                            {
                                var field = program.Strings[UnpackFirstOperand(opcode)];
                                var argCount = code[ip++];
                                ref var instance = ref Peek(argCount);
                                var function = instance[field];
                                if (!DoInstanceCall(function, argCount, ref code, ref ip, ref program, ref functionAddress))
                                {
                                    throw new MondRuntimeException(RuntimeError.FieldNotCallable, (string)field);
                                }
                                break;
                            }

                        case (int)InstructionType.Enter:
                            {
                                var localCount = UnpackFirstOperand(opcode);

                                var newFrame = _arrayPool.RentRaw(localCount);
                                PushLocal(newFrame);

                                locals = newFrame;
                                break;
                            }

                        case (int)InstructionType.Ret:
                            {
                                if (DoReturn(initialCallDepth, initialEvalDepth, initialEvalDirtyDepth, true,
                                        out program, out code, out ip, out functionAddress, out locals, out var returnValue))
                                {
                                    return returnValue;
                                }
                                break;
                            }

                        case (int)InstructionType.VarArgs:
                            {
                                var fixedCount = UnpackFirstOperand(opcode);
                                functionAddress.SetupVarArgs(fixedCount);
                                break;
                            }
                        #endregion

                        #region Branching
                        case (int)InstructionType.Jmp:
                            {
                                var address = UnpackFirstOperand(opcode);
                                ip = address;
                                break;
                            }

                        case (int)InstructionType.JmpTrueP:
                            {
                                var address = UnpackFirstOperand(opcode);

                                if (Peek())
                                    ip = address;

                                break;
                            }

                        case (int)InstructionType.JmpFalseP:
                            {
                                var address = UnpackFirstOperand(opcode);

                                if (!Peek())
                                    ip = address;

                                break;
                            }

                        case (int)InstructionType.JmpTrue:
                            {
                                var address = UnpackFirstOperand(opcode);

                                if (Pop())
                                    ip = address;

                                break;
                            }

                        case (int)InstructionType.JmpFalse:
                            {
                                var address = UnpackFirstOperand(opcode);

                                if (!Pop())
                                    ip = address;

                                break;
                            }

                        case (int)InstructionType.JmpTable:
                            {
                                var start = UnpackFirstOperand(opcode);
                                var count = code[ip++];
                                var endIp = ip + count;

                                var value = Pop();
                                if (value.Type == MondValueType.Number)
                                {
                                    var number = (double)value;
                                    var numberInt = (int)number;

                                    if (number >= start && number < start + count &&
                                        Math.Abs(number - numberInt) <= double.Epsilon)
                                    {
                                        ip = code[ip + (numberInt - start)];
                                        break;
                                    }
                                }

                                ip = endIp;
                                break;
                            }
                        #endregion

                        case (int)InstructionType.Breakpoint:
                            {
#if !NO_DEBUG
                                if (Debugger != null)
                                {
                                    DebuggerBreak(program, locals, functionAddress, ip, initialCallDepth);
                                }
#endif
                                break;
                            }

                        case (int)InstructionType.DebugCheckpoint:
                            {
#if !NO_DEBUG
                                if (Debugger != null)
                                {
                                    var shouldStopAtStmt =
                                        (_debugAction == MondDebugAction.StepInto) ||
                                        (_debugAction == MondDebugAction.StepOver && _debugDepth == 0) ||
                                        (_debugAction == MondDebugAction.StepOut && _debugDepth < 0);

                                    var shouldBreak = shouldStopAtStmt || Debugger.ShouldBreak(program, ip);

                                    if (shouldBreak)
                                        DebuggerBreak(program, locals, functionAddress, ip, initialCallDepth);
                                }
#endif
                                break;
                            }

                        default:
                            throw new MondRuntimeException(RuntimeError.UnhandledOpcode);
                    }
                }
            }
            catch (Exception e)
            {
                var message = e.Message.Trim();

                // we skip the OOB checks in the stack methods because the CLR has issues eliminating 
                // its own checks, so we let it throw and check here for a bit of a speed boost
                if (e is IndexOutOfRangeException)
                {
                    if (_callStackSize >= CallStackCapacity || _localStackSize >= CallStackCapacity || _evalStackSize >= EvalStackCapacity)
                    {
                        message = RuntimeError.StackOverflow;
                    }
                    else if (_callStackSize < 0 || _localStackSize < 0 || _evalStackSize < 0)
                    {
                        message = RuntimeError.StackEmpty;
                    }
                }

                StringBuilder stackTraceBuilder;

                if (e is MondRuntimeException runtimeException &&
                    runtimeException.MondStackTrace != null)
                {
                    stackTraceBuilder = new StringBuilder(runtimeException.MondStackTrace);

                    // check if we are running in a wrapped function
                    var stackTrace = new System.Diagnostics.StackTrace(e, false);
                    var frames = stackTrace.GetFrames();
                    var foundWrapper = false;

                    // skip the first frame because it's this method? need to verify
                    for (var i = 1; i < frames.Length; i++)
                    {
                        var method = frames[i].GetMethod();
                        if (method == null)
                            continue; // ???

                        var type = method.DeclaringType;

                        // stop at the next call to Machine.Run because it can be recursive
                        if (type == typeof(Machine) && method.Name == "Run")
                            break;

                        // the wrapper is a lambda so it's in a compiler generated type, which will be nested
                        var parentType = type.DeclaringType;
                        if (parentType == null)
                            continue;

                        // the type and method are compiler generated so they have a weird (and compiler specific) name
                        const string wrapperMagic = "<CheckWrapFunction>";

                        // make sure the type is nested in MondValue and check both the type and method name
                        if (parentType == typeof(MondValue) && (method.Name.Contains(wrapperMagic) || type.Name.Contains(wrapperMagic)))
                        {
                            foundWrapper = true;
                            break;
                        }
                    }

                    // don't show a native transition for wrappers
                    if (!foundWrapper)
                        stackTraceBuilder.AppendLine("[... native ...]");
                }
                else
                {
                    stackTraceBuilder = new StringBuilder();
                }

                // first line of the stack trace is where we are running
                stackTraceBuilder.AppendLine(GetAddressDebugInfo(program, errorIp));

                // generate stack trace and reset stacks
                for (var i = Math.Min(_callStackSize - 1, CallStackCapacity - 1); i > initialCallDepth; i--)
                {
                    var returnAddress = _callStack[i];
                    stackTraceBuilder.AppendLine(GetAddressDebugInfo(returnAddress.Program, returnAddress.Address));
                }

                _callStackSize = initialCallDepth;
                for (var i = _callStackSize + 1; i < CallStackCapacity; i++)
                {
                    _callStack[i] = default;
                }

                _localStackSize = initialLocalDepth;
                for (var i = _localStackSize + 1; i < CallStackCapacity; i++)
                {
                    _localStack[i] = default;
                }

                _evalStackSize = initialEvalDepth;
                _evalStackDirtySize = initialEvalDirtyDepth;
                for (var i = _evalStackDirtySize + 1; i < EvalStackCapacity; i++)
                {
                    _evalStack[i] = default;
                }

                throw new MondRuntimeException(message, e)
                {
                    MondStackTrace = stackTraceBuilder.ToString()
                };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int UnpackFirstOperand(int opcode)
        {
            const int operandMask    = 0x00FFFFFF;
            const int operandSignBit = 0x00800000;

            return ((opcode & operandMask) ^ operandSignBit) - operandSignBit;
        }

        private void DoCall(int argCount, ref int[] code, ref int ip, ref MondProgram program, ref ReturnAddress funcAddress, ref MondValue[] locals)
        {
            var unpackCount = code[ip++];
            using var argValuesHandle = GetArgsArray(code, ref ip, argCount, unpackCount);
            var argValues = argValuesHandle.Span;
            var function = Pop();

            var returnAddress = ip;

            if (function.Type == MondValueType.Object)
            {
                using var argArrHandle = _arrayPool.Rent(argValues.Length + 1);
                var argArr = argArrHandle.Span;
                argValues.CopyTo(argArr[1..]);
                argArr[0] = function;

                if (function.TryDispatch("__call", out var result, argArr))
                {
                    Push(result);
                    return;
                }
            }

            if (function.Type != MondValueType.Function)
            {
                throw new MondRuntimeException(RuntimeError.ValueNotCallable, function.Type.GetName());
            }

            CallImpl(function.FunctionValue, argValues, returnAddress, ref program, ref code, ref ip, ref funcAddress);
        }

        private bool DoInstanceCall(MondValue function, int argCount, ref int[] code, ref int ip, ref MondProgram program, ref ReturnAddress funcAddress)
        {
            var unpackCount = code[ip++];
            using var argValuesHandle = GetArgsArray(code, ref ip, argCount, unpackCount, 1);
            var argValues = argValuesHandle.Span;
            var instance = Pop();
            argValues[0] = instance;

            var returnAddress = ip;

            if (function.Type == MondValueType.Object)
            {
                using var argArrHandle = _arrayPool.Rent(argValues.Length + 1);
                var argArr = argArrHandle.Span;
                argValues.CopyTo(argArr[1..]);
                argArr[0] = function;

                if (function.TryDispatch("__call", out var result, argArr))
                {
                    Push(result);
                    return true;
                }
            }

            if (function.Type != MondValueType.Function)
            {
                return false;
            }

            CallImpl(function.FunctionValue, argValues, returnAddress, ref program, ref code, ref ip, ref funcAddress);
            return true;
        }

        private void CallImpl(Closure closure, Span<MondValue> argValues, int returnAddress, ref MondProgram program,
            ref int[] code, ref int ip, ref ReturnAddress funcAddress)
        {
            switch (closure.Type)
            {
                case ClosureType.Mond:
                    var newFuncAddress = PushCall();
                    newFuncAddress.Initialize(program, returnAddress, closure, (short)_evalStackSize, false);
                    foreach (var arg in argValues)
                    {
                        newFuncAddress.Arguments.Add(arg);
                    }

                    program = closure.Program;
                    code = program.Bytecode;
                    ip = closure.Address;
                    funcAddress = newFuncAddress;

                    DebuggerOnCall();
                    break;

                case ClosureType.Native:
                    var result = closure.NativeFunction(_state, argValues);
                    Push(result);
                    break;

                default:
                    throw new MondRuntimeException(RuntimeError.UnhandledClosureType);
            }
        }

        private ArrayPoolHandle<MondValue> GetArgsArray(int[] code, ref int ip, int argCount, int unpackCount, int offset = 0)
        {
            if (argCount == 0 && unpackCount == 0 && offset == 0)
            {
                return _arrayPool.Rent(0);
            }

            if (unpackCount > 0)
            {
                var unpackArgs = UnpackArgs(code, ref ip, argCount, unpackCount);
                var unpackHandle = _arrayPool.Rent(unpackArgs.Count + offset);
                var unpackSpan = unpackHandle.Span;
                for (var i = 0; i < unpackArgs.Count; i++)
                {
                    unpackSpan[offset + i] = unpackArgs[i];
                }

                return unpackHandle;
            }

            var handle = _arrayPool.Rent(argCount + offset);
            var span = handle.Span;
            for (var i = argCount - 1; i >= 0; i--)
            {
                span[offset + i] = Pop();
            }

            return handle;
        }

        private List<MondValue> UnpackArgs(int[] code, ref int ip, int argCount, int unpackCount)
        {
            var unpackIndices = unpackCount < 32 
                ? stackalloc int[unpackCount]
                : new int[unpackCount];

            for (var i = 0; i < unpackCount; i++)
            {
                unpackIndices[i] = code[ip++];
            }

            var unpackedArgs = new List<MondValue>(argCount + unpackCount * 16);
            var argIndex = 0;
            var unpackIndex = 0;

            for (var i = argCount - 1; i >= 0; i--)
            {
                var value = Pop();

                if (unpackIndex < unpackIndices.Length && i == unpackIndices[unpackIndex])
                {
                    unpackIndex++;

                    var start = argIndex;
                    var count = 0;

                    foreach (var unpackedValue in value.Enumerate(_state))
                    {
                        unpackedArgs.Add(unpackedValue);
                        argIndex++;
                        count++;
                    }

                    unpackedArgs.Reverse(start, count);

                    continue;
                }

                unpackedArgs.Add(value);
                argIndex++;
            }

            unpackedArgs.Reverse();
            return unpackedArgs;
        }

        private bool DoReturn(int initialCallDepth, int initialEvalDepth, int initialEvalDirtyDepth, bool returnLocalsToPool,
            out MondProgram program, out int[] code, out int ip, out ReturnAddress functionAddress, out MondValue[] locals, out MondValue returnValue)
        {
            var returnAddress = PopCall();
            PopLocal(returnLocalsToPool);

            program = returnAddress.Program;
            code = program.Bytecode;
            ip = returnAddress.Address;

            functionAddress = _callStackSize >= 0 ? PeekCall() : null;
            locals = _localStackSize >= 0 ? PeekLocal() : null;

            DebuggerOnReturn();

            if (_callStackSize == initialCallDepth)
            {
                var result = Pop();
                ClearDirty();

                // restore the previous stack offsets
                _evalStackSize = initialEvalDepth;
                _evalStackDirtySize = initialEvalDirtyDepth;

                returnValue = result;
                return true;
            }

            ClearDirty();

            returnValue = MondValue.Undefined;
            return false;
        }

        private void DebuggerOnCall()
        {
#if !NO_DEBUG
            if (Debugger != null && _debugAction != MondDebugAction.Run)
            {
                _debugDepth++;
            }
#endif
        }

        private void DebuggerOnReturn()
        {
#if !NO_DEBUG
            if (Debugger != null && _debugAction != MondDebugAction.Run)
            {
                _debugDepth--;
            }
#endif
        }

#if !NO_DEBUG
        private void DebuggerBreak(MondProgram program, MondValue[] locals, ReturnAddress args, int address, int initialCallDepth)
        {
            var context = new MondDebugContext(
                _state, program, address, locals, args, _callStack, _callStackSize, initialCallDepth);

            // so eval can work
            _debugAction = MondDebugAction.Run;

            _debugAction = Debugger.Break(context, address);
            _debugDepth = 0;
        }
#endif

        private static string GetAddressDebugInfo(MondProgram program, int address)
        {
            if (program.DebugInfo != null)
            {
                var func = program.DebugInfo.FindFunction(address);
                var position = program.DebugInfo.FindPosition(address);

                if (func.HasValue && position.HasValue)
                {
                    var prefix = "";
                    var funcName = program.Strings[func.Value.Name];
                    var fileName = program.DebugInfo.FileName ?? program.GetHashCode().ToString("X8");

                    if (!string.IsNullOrEmpty(funcName))
                        prefix = string.Format("at {0} ", funcName);

                    return string.Format("{0}in {1}: line {2}:{3}", prefix, fileName, position.Value.LineNumber, position.Value.ColumnNumber);
                }
            }

            return address.ToString("X8");
        }
    }
}
