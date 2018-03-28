using System;
using System.Collections.Generic;
using Mond.Compiler.Expressions;

namespace Mond.Compiler
{
    partial class FunctionContext
    {
        public void Function(string name = null)
        {
            if (Compiler.Options.DebugInfo < MondDebugInfoLevel.StackTrace)
                return;

            Emit(new Instruction(InstructionType.Function, String(name)));
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
                if (identifier.FrameIndex != LocalIndex)
                    Emit(new Instruction(InstructionType.LdLoc, operand));
                else
                    Emit(new Instruction(InstructionType.LdLocF, new ImmediateOperand(identifier.Id)));

                return 1;
            }

            throw new NotSupportedException();
        }

        public int LoadGlobal()
        {
            Emit(new Instruction(InstructionType.LdGlobal));
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
            if (operand.FrameIndex != LocalIndex)
                Emit(new Instruction(InstructionType.StLoc, operand));
            else
                Emit(new Instruction(InstructionType.StLocF, new ImmediateOperand(operand.Id)));

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

        public int LoadState(int depth)
        {
            Emit(new Instruction(InstructionType.LdState, new ImmediateOperand(depth)));
            return 0;
        }

        public int StoreState(int depth)
        {
            Emit(new Instruction(InstructionType.StState, new ImmediateOperand(depth)));
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
            return -length + 1;
        }

        public int Slice()
        {
            Emit(new Instruction(InstructionType.Slice));
            return -4 + 1;
        }

        public int FlushArray(int length)
        {
            Emit(new Instruction(InstructionType.FlushArr, new ImmediateOperand(length)));
            return -length;
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

        public int Closure(LabelOperand label)
        {
            Emit(new Instruction(InstructionType.Closure, label));
            return 1;
        }

        public int Call(int argumentCount, List<ImmediateOperand> unpackIndices)
        {
            Emit(new Instruction(
                InstructionType.Call,
                new ImmediateOperand(argumentCount),
                new ImmediateByteOperand((byte)unpackIndices.Count),
                new ListOperand<ImmediateOperand>(unpackIndices)));

            return -argumentCount - 1 + 1;
        }

        public int TailCall(int argumentCount, LabelOperand label, List<ImmediateOperand> unpackIndices)
        {
            Emit(new Instruction(
                InstructionType.TailCall,
                new ImmediateOperand(argumentCount),
                label,
                new ImmediateByteOperand((byte)unpackIndices.Count),
                new ListOperand<ImmediateOperand>(unpackIndices)));

            return -argumentCount;
        }

        public int Return()
        {
            Emit(new Instruction(InstructionType.Ret));
            return -1;
        }

        public int Enter()
        {
            var identifierCount = new DeferredOperand<ImmediateOperand>(() =>
                new ImmediateOperand(IdentifierCount));

            Emit(new Instruction(InstructionType.Enter, identifierCount));
            return 0;
        }

        public int Leave()
        {
            Emit(new Instruction(InstructionType.Leave));
            return 0;
        }

        public int VarArgs(int fixedCount)
        {
            Emit(new Instruction(InstructionType.VarArgs, new ImmediateOperand(fixedCount)));
            return 0;
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
