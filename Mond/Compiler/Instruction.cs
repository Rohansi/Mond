﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Mond.Compiler
{
    enum InstructionType : byte
    {
        Function,                           // debug info
        Position,                           // debug info
        Label,                              // label binding

        Dup, Drop, Swap,                    // duplicate, drop, swap

        LdUndef, LdNull,                    // load undefined/null constant
        LdTrue, LdFalse,                    // load true/false constant
        LdNum, LdStr,                       // load number/string constant
        LdGlobal,                           // load global object

        LdLocF, StLocF,                     // load/store local in current frame
        LdLoc, StLoc,                       // load/store local
        LdFld, StFld,                       // load/store field
        LdArr, StArr,                       // load/store array
        LdState, StState,                   // load/store current frame stack and evals in another frame

        NewObject, NewArray,                // create object/array
        Slice,                              // slice array

        Add, Sub, Mul, Div, Mod, Exp,       // math
        Neg,                                // negate
        Eq, Neq, Gt, Gte, Lt, Lte,          // comparison operators
        Not,                                // logical not

        In, NotIn,                          // contains, !contains

        BitLShift, BitRShift,               // bitwise shift
        BitAnd, BitOr, BitXor,              // bitwise and, bitwise or, bitwise xor
        BitNot,                             // bitwise not (one's compliment)

        Closure, Call, TailCall,            // make closure, call function, tail call
        Enter, Leave, Ret,                  // push locals/begin function, pop locals, pop locals and return from function
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
                    Type == InstructionType.Position)
                {
                    return 0;
                }

                return 1 + Operands.Sum(o => o.Length);
            }
        }

        public void Print()
        {
            if (Type == InstructionType.Function || Type == InstructionType.Position)
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
                Type == InstructionType.Position)
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
