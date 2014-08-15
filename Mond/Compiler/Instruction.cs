using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Mond.Compiler
{
    enum InstructionType : byte
    {
        Function,                           // debug info
        Line,                               // debug info
        Label,                              // label binding

        Dup, Drop, Swap,                    // duplicate, drop, swap

        LdUndef, LdNull,                    // load undefined/null constant
        LdTrue, LdFalse,                    // load true/false constant
        LdNum, LdStr,                       // load number/string constant
        LdGlobal,                           // load global object

        LdLoc, StLoc,                       // load/store local
        LdFld, StFld,                       // load/store field
        LdArr, StArr,                       // load/store array

        NewObject, NewArray,                // create object/array

        Add, Sub, Mul, Div, Mod,            // math
        Neg,                                // negate
        Eq, Neq, Gt, Gte, Lt, Lte,          // comparison operators
        Not,                                // logical not

        Closure, Call, TailCall,            // make closure, call closure, tail call
        Enter, Ret,                         // begin function, return from function
        VarArgs,                            // setup variable length args

        Jmp,                                // jump unconditionally
        JmpTrueP, JmpFalseP,                // jump if peek() == true/false
        JmpTrue, JmpFalse,                  // jump if pop() == true/false
        JmpTable,
    }

    class Instruction
    {
        private int _offset;

        public readonly InstructionType Type;
        public readonly ReadOnlyCollection<IInstructionOperand> Operands;

        public Instruction(InstructionType type, params IInstructionOperand[] operands)
        {
            Type = type;
            Operands = new ReadOnlyCollection<IInstructionOperand>(operands);
        }

        public Instruction(InstructionType type, List<IInstructionOperand> operands)
        {
            Type = type;
            Operands = operands.AsReadOnly();
        }

        public int Offset
        {
            get { return _offset; }
            set
            {
                if (Type == InstructionType.Label)
                {
                    var label = (LabelOperand)Operands[0];
                    label.Position = value;
                }

                _offset = value;
            }
        }

        public int Length
        {
            get
            {
                if (Type == InstructionType.Label ||
                    Type == InstructionType.Function ||
                    Type == InstructionType.Line)
                {
                    return 0;
                }

                return 1 + Operands.Sum(o => o.Length);
            }
        }

        public void Print()
        {
            if (Type == InstructionType.Function || Type == InstructionType.Line)
                return;

            if (Type == InstructionType.Label)
                Console.ForegroundColor = ConsoleColor.DarkGreen;

            Console.Write("{0:X4} {1,-15} ", Offset, Type.ToString().ToLower());

            foreach (var operand in Operands)
            {
                operand.Print();
            }

            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public void Write(BinaryWriter writer)
        {
            if (Type == InstructionType.Label ||
                Type == InstructionType.Function ||
                Type == InstructionType.Line)
            {
                return;
            }

            writer.Write((byte)Type);

            foreach (var operand in Operands)
            {
                operand.Write(writer);
            }
        }
    }
}
