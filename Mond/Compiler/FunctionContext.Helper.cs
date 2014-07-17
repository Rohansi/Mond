using System;
using System.Collections.Generic;

namespace Mond.Compiler
{
    partial class FunctionContext
    {
        private int _lambdaId;

        public void Function(string fileName, string name = null)
        {
            if (!Compiler.GeneratingDebugInfo)
                return;

            name = name ?? string.Format("lambda_{0}", _lambdaId++);
            Emit(new Instruction(InstructionType.Function, String(name), String(fileName ?? "null")));
        }

        public void Line(string fileName, int line)
        {
            if (!Compiler.GeneratingDebugInfo)
                return;

            Emit(new Instruction(InstructionType.Line, String(fileName ?? "null"), new ImmediateOperand(line)));
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

            if (operand is IdentifierOperand)
            {
                Emit(new Instruction(InstructionType.LdLoc, operand));
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
            Emit(new Instruction(InstructionType.StLoc, operand));
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

        public int Dup()
        {
            Emit(new Instruction(InstructionType.Dup));
            return 1;
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

        public int BinaryOperation(TokenType operation)
        {
            InstructionType type;
            if (!_binaryOperationMap.TryGetValue(operation, out type))
                throw new NotSupportedException();

            Emit(new Instruction(type));
            return -2 + 1;
        }

        public int UnaryOperation(TokenType operation)
        {
            InstructionType type;
            if (!_unaryOperationMap.TryGetValue(operation, out type))
                throw new NotSupportedException();

            Emit(new Instruction(type));
            return -1 + 1;
        }

        public int Closure(LabelOperand label)
        {
            Emit(new Instruction(InstructionType.Closure, label));
            return 1;
        }

        public int Call(int argumentCount)
        {
            Emit(new Instruction(InstructionType.Call, new ImmediateOperand(argumentCount)));
            return -argumentCount - 1 + 1;
        }

        public int TailCall(int argumentCount, LabelOperand label)
        {
            Emit(new Instruction(InstructionType.TailCall, new ImmediateOperand(argumentCount), label));
            return -argumentCount;
        }

        public int Return()
        {
            Emit(new Instruction(InstructionType.Ret));
            return -1;
        }

        public int Enter()
        {
            Emit(new Instruction(InstructionType.Enter));
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
            var count = new DeferredImmediateOperand(() => labels.Count);
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

                { TokenType.EqualTo, InstructionType.Eq },
                { TokenType.NotEqualTo, InstructionType.Neq },
                { TokenType.GreaterThan, InstructionType.Gt },
                { TokenType.GreaterThanOrEqual, InstructionType.Gte },
                { TokenType.LessThan, InstructionType.Lt },
                { TokenType.LessThanOrEqual, InstructionType.Lte }
            };

            _unaryOperationMap = new Dictionary<TokenType, InstructionType>
            {
                { TokenType.Subtract, InstructionType.Neg },

                { TokenType.LogicalNot, InstructionType.Not }
            };
        }
    }
}
