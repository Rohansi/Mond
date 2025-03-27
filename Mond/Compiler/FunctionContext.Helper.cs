using System;
using System.Collections.Generic;
using Mond.Compiler.Expressions;

namespace Mond.Compiler
{
    internal partial class FunctionContext
    {
        public void Function(string functionName)
        {
            if (Compiler.Options.DebugInfo < MondDebugInfoLevel.StackTrace)
                return;

            Emit(new Instruction(InstructionType.Function, String(functionName ?? "")));
        }

        public void Position(Token token)
        {
            if (Compiler.Options.DebugInfo < MondDebugInfoLevel.StackTrace)
                return;

            Emit(new Instruction(InstructionType.Position, new ImmediateOperand(token.Line), new ImmediateOperand(token.Column)));
        }

        public void Statement(Token start, Token end)
        {
            if (Compiler.Options.DebugInfo < MondDebugInfoLevel.Full)
                return;

            Emit(new Instruction(InstructionType.Statement, new IInstructionOperand[]
            {
                new ImmediateOperand(start.Line),
                new ImmediateOperand(start.Column),
                new ImmediateOperand(end.Line),
                new ImmediateOperand(end.Column + end.Contents.Length - 1)
            }));

            if (start.Line > 0 && end.Line > 0)
                Emit(new Instruction(InstructionType.DebugCheckpoint));
        }

        public void Statement(Expression expression)
        {
            Statement(expression.StartToken, expression.EndToken);
        }

        public int Breakpoint()
        {
            Emit(new Instruction(InstructionType.Breakpoint));
            return 0;
        }

        public int Bind(LabelOperand label)
        {
            Emit(new Instruction(InstructionType.Label, label));
            return 0;
        }

        public int LoadUndefined()
        {
            Emit(new Instruction(InstructionType.LdUndef));
            return 1;
        }

        public int LoadNull()
        {
            Emit(new Instruction(InstructionType.LdNull));
            return 1;
        }

        public int LoadTrue()
        {
            Emit(new Instruction(InstructionType.LdTrue));
            return 1;
        }

        public int LoadFalse()
        {
            Emit(new Instruction(InstructionType.LdFalse));
            return 1;
        }

        public int Load(IInstructionOperand operand)
        {
            if (operand is ConstantOperand<double>)
            {
                Emit(new Instruction(InstructionType.LdNum, operand));
                return 1;
            }

            if (operand is ConstantOperand<string>)
            {
                Emit(new Instruction(InstructionType.LdStr, operand));
                return 1;
            }

            if (operand is IdentifierOperand identifier)
            {
                if (identifier.IsGlobal)
                {
                    Emit(new Instruction(InstructionType.LdGlobalFld, String(identifier.Name)));
                }
                else if (identifier.FrameIndex == FrameDepth)
                {
                    if (identifier.IsCaptured)
                    {
                        Emit(new Instruction(InstructionType.LdArrF,
                            new ImmediateOperand(identifier.CaptureArray.Id), new ImmediateOperand(identifier.Id)));
                    }
                    else if (operand is ArgumentIdentifierOperand)
                    {
                        Emit(new Instruction(InstructionType.LdArgF, new ImmediateOperand(identifier.Id)));
                    }
                    else
                    {
                        Emit(new Instruction(InstructionType.LdLocF, new ImmediateOperand(identifier.Id)));
                    }
                }
                else if (identifier.IsCaptured)
                {
                    Emit(new Instruction(InstructionType.LdUpValue,
                        new ImmediateOperand(identifier.Scope.LexicalDepth), new ImmediateOperand(identifier.Id)));
                }
                else
                {
                    throw new InvalidOperationException();
                }

                return 1;
            }

            throw new NotSupportedException();
        }

        public int LoadGlobal()
        {
            Emit(new Instruction(InstructionType.LdGlobal));
            return 1;
        }

        public int LoadGlobalField(ConstantOperand<string> operand)
        {
            Emit(new Instruction(InstructionType.LdGlobalFld, operand));
            return 1;
        }

        public int LoadField(ConstantOperand<string> operand)
        {
            Emit(new Instruction(InstructionType.LdFld, operand));
            return -1 + 1;
        }

        public int LoadArray()
        {
            Emit(new Instruction(InstructionType.LdArr));
            return -2 + 1;
        }

        public int Store(IdentifierOperand operand)
        {
            if (operand.IsGlobal)
            {
                // should always be readonly
                throw new InvalidOperationException();
            } 
            
            if (operand.FrameIndex == FrameDepth)
            {
                if (operand.IsCaptured)
                {
                    Emit(new Instruction(InstructionType.StArrF,
                        new ImmediateOperand(operand.CaptureArray.Id), new ImmediateOperand(operand.Id)));
                }
                else if (operand is ArgumentIdentifierOperand)
                {
                    Emit(new Instruction(InstructionType.StArgF, new ImmediateOperand(operand.Id)));
                }
                else
                {
                    Emit(new Instruction(InstructionType.StLocF, new ImmediateOperand(operand.Id)));
                }
            }
            else if (operand.IsCaptured)
            {
                Emit(new Instruction(InstructionType.StUpValue, new ImmediateOperand(operand.Scope.LexicalDepth), new ImmediateOperand(operand.Id)));
            }
            else
            {
                throw new InvalidOperationException();
            }

            return -1;
        }

        public int StoreField(ConstantOperand<string> operand)
        {
            Emit(new Instruction(InstructionType.StFld, operand));
            return -2;
        }

        public int StoreArray()
        {
            Emit(new Instruction(InstructionType.StArr));
            return -3;
        }

        public int SeqSuspend()
        {
            Emit(new Instruction(InstructionType.SeqSuspend));
            return 0;
        }

        public int SeqResume()
        {
            Emit(new Instruction(InstructionType.SeqResume));
            return 0;
        }

        public int NewObject()
        {
            Emit(new Instruction(InstructionType.NewObject));
            return 1;
        }

        public int NewArray(int length)
        {
            Emit(new Instruction(InstructionType.NewArray, new ImmediateOperand(length)));
            return 1;
        }

        public int Slice()
        {
            Emit(new Instruction(InstructionType.Slice));
            return -4 + 1;
        }

        public int Dup()
        {
            Emit(new Instruction(InstructionType.Dup));
            return 1;
        }

        public int Dup2()
        {
            Emit(new Instruction(InstructionType.Dup2));
            return 2;
        }

        public int Drop()
        {
            Emit(new Instruction(InstructionType.Drop));
            return -1;
        }

        public int Swap()
        {
            Emit(new Instruction(InstructionType.Swap));
            return 0;
        }

        public int Swap1For2()
        {
            Emit(new Instruction(InstructionType.Swap1For2));
            return 0;
        }

        public int BinaryOperation(TokenType operation)
        {
            if (!_binaryOperationMap.TryGetValue(operation, out var type))
                throw new NotSupportedException();

            Emit(new Instruction(type));
            return -2 + 1;
        }

        public int UnaryOperation(TokenType operation)
        {
            if (!_unaryOperationMap.TryGetValue(operation, out var type))
                throw new NotSupportedException();

            Emit(new Instruction(type));
            return -1 + 1;
        }

        public int IncrementF(IdentifierOperand local)
        {
            if (local.FrameIndex != FrameDepth)
                throw new ArgumentException("Cannot use IncF on out of frame locals");

            Emit(new Instruction(InstructionType.IncF, new ImmediateOperand(local.Id)));
            return 0;
        }

        public int DecrementF(IdentifierOperand local)
        {
            if (local.FrameIndex != FrameDepth)
                throw new ArgumentException("Cannot use DecF on out of frame locals");

            Emit(new Instruction(InstructionType.DecF, new ImmediateOperand(local.Id)));
            return 0;
        }

        public int Closure(LabelOperand label)
        {
            var callingFrameDepth = FrameDepth;

            var upFrameCount = 0;
            var scope = Scope;
            while (scope != null)
            {
                if (scope.FrameDepth != callingFrameDepth)
                {
                    Emit(new Instruction(InstructionType.LdUp, new ImmediateOperand(scope.LexicalDepth)));
                }
                else if (scope.CaptureArray != null)
                {
                    Load(scope.CaptureArray);
                }
                else
                {
                    LoadUndefined();
                }

                scope = scope.Previous;
                upFrameCount++;
            }

            if (upFrameCount != Scope.LexicalDepth + 1)
            {
                throw new InvalidOperationException();
            }

            Emit(new Instruction(InstructionType.Closure, new ImmediateOperand(upFrameCount), label));
            return 1;
        }

        public int Call(int argumentCount, List<ImmediateOperand> unpackIndices)
        {
            Emit(new Instruction(
                InstructionType.Call,
                new ImmediateOperand(argumentCount),
                new ImmediateOperand(unpackIndices.Count),
                new ListOperand<ImmediateOperand>(unpackIndices)));

            return -argumentCount - 1 + 1; // pop arguments, pop function, push result
        }

        public int TailCall(int argumentCount, LabelOperand label, List<ImmediateOperand> unpackIndices)
        {
            Emit(new Instruction(
                InstructionType.TailCall,
                new ImmediateOperand(argumentCount),
                label,
                new ImmediateOperand(unpackIndices.Count),
                new ListOperand<ImmediateOperand>(unpackIndices)));

            return -argumentCount; // pop arguments
        }

        public int InstanceCall(ConstantOperand<string> field, int argumentCount, List<ImmediateOperand> unpackIndices)
        {
            Emit(new Instruction(
                InstructionType.InstanceCall,
                field,
                new ImmediateOperand(argumentCount),
                new ImmediateOperand(unpackIndices.Count),
                new ListOperand<ImmediateOperand>(unpackIndices)));

            return -argumentCount - 1 + 1; // pop arguments, pop instance, push result
        }

        public int Return()
        {
            Emit(new Instruction(InstructionType.Ret));
            return -1;
        }

        public int Jump(LabelOperand label)
        {
            Emit(new Instruction(InstructionType.Jmp, label));
            return 0;
        }

        public int JumpTrue(LabelOperand label)
        {
            Emit(new Instruction(InstructionType.JmpTrue, label));
            return -1;
        }

        public int JumpFalse(LabelOperand label)
        {
            Emit(new Instruction(InstructionType.JmpFalse, label));
            return -1;
        }

        public int JumpTruePeek(LabelOperand label)
        {
            Emit(new Instruction(InstructionType.JmpTrueP, label));
            return 0;
        }

        public int JumpFalsePeek(LabelOperand label)
        {
            Emit(new Instruction(InstructionType.JmpFalseP, label));
            return 0;
        }

        public int JumpTable(int start, List<LabelOperand> labels)
        {
            var startOp = new ImmediateOperand(start);

            var count = new DeferredOperand<ImmediateOperand>(() =>
                new ImmediateOperand(labels.Count));

            var list = new ListOperand<LabelOperand>(labels);

            Emit(new Instruction(InstructionType.JmpTable, startOp, count, list));
            return -1;
        }

        private static Dictionary<TokenType, InstructionType> _binaryOperationMap;
        private static Dictionary<TokenType, InstructionType> _unaryOperationMap;

        static FunctionContext()
        {
            _binaryOperationMap = new Dictionary<TokenType, InstructionType>
            {
                { TokenType.Add, InstructionType.Add },
                { TokenType.Subtract, InstructionType.Sub },
                { TokenType.Multiply, InstructionType.Mul },
                { TokenType.Divide, InstructionType.Div },
                { TokenType.Modulo, InstructionType.Mod },
                { TokenType.Exponent, InstructionType.Exp },

                { TokenType.BitLeftShift, InstructionType.BitLShift },
                { TokenType.BitRightShift, InstructionType.BitRShift },
                { TokenType.BitAnd, InstructionType.BitAnd },
                { TokenType.BitOr, InstructionType.BitOr },
                { TokenType.BitXor, InstructionType.BitXor },

                { TokenType.EqualTo, InstructionType.Eq },
                { TokenType.NotEqualTo, InstructionType.Neq },
                { TokenType.GreaterThan, InstructionType.Gt },
                { TokenType.GreaterThanOrEqual, InstructionType.Gte },
                { TokenType.LessThan, InstructionType.Lt },
                { TokenType.LessThanOrEqual, InstructionType.Lte },
                { TokenType.In, InstructionType.In },
                { TokenType.NotIn, InstructionType.NotIn }
            };

            _unaryOperationMap = new Dictionary<TokenType, InstructionType>
            {
                { TokenType.Subtract, InstructionType.Neg },
                { TokenType.BitNot, InstructionType.BitNot },

                { TokenType.Not, InstructionType.Not }
            };
        }
    }
}
