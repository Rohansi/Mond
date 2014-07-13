using System;
using System.Collections.Generic;

namespace Mond.Compiler
{
    partial class CompilerContext
    {
        private int _lambdaId;

        public void Function(string fileName, string name = null)
        {
            if (!_compiler.GeneratingDebugInfo)
                return;

            name = name ?? string.Format("lambda_{0}", _lambdaId++);
            Emit(new Instruction(InstructionType.Function, String(name), String(fileName ?? "null")));
        }

        public void Line(string fileName, int line)
        {
            if (!_compiler.GeneratingDebugInfo)
                return;

            Emit(new Instruction(InstructionType.Line, String(fileName ?? "null"), new ImmediateOperand(line)));
        }

        public void Bind(LabelOperand label)
        {
            Emit(new Instruction(InstructionType.Label, label));
        }

        public void LoadUndefined()
        {
            Emit(new Instruction(InstructionType.LdUndef));
        }

        public void LoadNull()
        {
            Emit(new Instruction(InstructionType.LdNull));
        }

        public void LoadTrue()
        {
            Emit(new Instruction(InstructionType.LdTrue));
        }

        public void LoadFalse()
        {
            Emit(new Instruction(InstructionType.LdFalse));
        }

        public void Load(IInstructionOperand operand)
        {
            InstructionType type;
            if (operand is ConstantOperand<double>)
                type = InstructionType.LdNum;
            else if (operand is ConstantOperand<string>)
                type = InstructionType.LdStr;
            else if (operand is IdentifierOperand)
                type = InstructionType.LdLoc;
            else
                throw new NotSupportedException();

            Emit(new Instruction(type, operand));
        }

        public void LoadGlobal()
        {
            Emit(new Instruction(InstructionType.LdGlobal));
        }

        public void LoadField(ConstantOperand<string> operand)
        {
            Emit(new Instruction(InstructionType.LdFld, operand));
        }

        public void LoadArray()
        {
            Emit(new Instruction(InstructionType.LdArr));
        }

        public void Store(IdentifierOperand operand)
        {
            Emit(new Instruction(InstructionType.StLoc, operand));
        }

        public void StoreField(ConstantOperand<string> operand)
        {
            Emit(new Instruction(InstructionType.StFld, operand));
        }

        public void StoreArray()
        {
            Emit(new Instruction(InstructionType.StArr));
        }

        public void NewObject()
        {
            Emit(new Instruction(InstructionType.NewObject));
        }

        public void NewArray(int length)
        {
            Emit(new Instruction(InstructionType.NewArray, new ImmediateOperand(length)));
        }

        public void Dup()
        {
            Emit(new Instruction(InstructionType.Dup));
        }

        public void Drop()
        {
            Emit(new Instruction(InstructionType.Drop));
        }

        public void Swap()
        {
            Emit(new Instruction(InstructionType.Swap));
        }

        public void BinaryOperation(TokenType operation)
        {
            InstructionType type;
            if (!_binaryOperationMap.TryGetValue(operation, out type))
                throw new NotSupportedException();

            Emit(new Instruction(type));
        }

        public void UnaryOperation(TokenType operation)
        {
            InstructionType type;
            if (!_unaryOperationMap.TryGetValue(operation, out type))
                throw new NotSupportedException();

            Emit(new Instruction(type));
        }

        public void Closure(LabelOperand label)
        {
            Emit(new Instruction(InstructionType.Closure, label));
        }

        public void Call(int argumentCount)
        {
            Emit(new Instruction(InstructionType.Call, new ImmediateOperand(argumentCount)));
        }

        public void Return()
        {
            Emit(new Instruction(InstructionType.Ret));
        }

        public void Enter()
        {
            Emit(new Instruction(InstructionType.Enter));
        }

        public void Jump(LabelOperand label)
        {
            Emit(new Instruction(InstructionType.Jmp, label));
        }

        public void JumpTrue(LabelOperand label)
        {
            Emit(new Instruction(InstructionType.JmpTrue, label));
        }

        public void JumpFalse(LabelOperand label)
        {
            Emit(new Instruction(InstructionType.JmpFalse, label));
        }

        public void JumpTruePeek(LabelOperand label)
        {
            Emit(new Instruction(InstructionType.JmpTrueP, label));
        }

        public void JumpFalsePeek(LabelOperand label)
        {
            Emit(new Instruction(InstructionType.JmpFalseP, label));
        }

        private static Dictionary<TokenType, InstructionType> _binaryOperationMap;
        private static Dictionary<TokenType, InstructionType> _unaryOperationMap; 

        static CompilerContext()
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
