using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mond.Compiler.Expressions;
using Mond.VirtualMachine;

namespace Mond.Compiler
{
    class ExpressionCompiler
    {
        private readonly List<FunctionContext> _contexts;
        private Scope _scope;
        private int _labelIndex;
        private List<Instruction> _instructions; 

        public readonly MondCompilerOptions Options;

        public readonly ConstantPool<double> NumberPool;
        public readonly ConstantPool<string> StringPool;

        public int LambdaId;
         
        public ExpressionCompiler(MondCompilerOptions options)
        {
            _contexts = new List<FunctionContext>();
            _scope = new Scope(0, null);
            _labelIndex = 0;

            Options = options;

            NumberPool = new ConstantPool<double>();
            StringPool = new ConstantPool<string>();

            LambdaId = 0;
        }

        public MondProgram Compile(Expression expression)
        {
            var context = new FunctionContext(this, 0, _scope, null, null);
            RegisterFunction(context);

            context.Function(context.FullName);

            context.Enter();
            expression.Compile(context);
            context.LoadUndefined();
            context.Return();

            var length = PatchLabels();
            var bytecode = GenerateBytecode(length);
            var debugInfo = GenerateDebugInfo();

            return new MondProgram(bytecode, NumberPool.Items, StringPool.Items, debugInfo);
        }

        private int PatchLabels()
        {
            var offset = 0;

            foreach (var instruction in AllInstructions())
            {
                instruction.Offset = offset;
                offset += instruction.Length;
            }

            return offset;
        }

        private byte[] GenerateBytecode(int bufferSize)
        {
            var bytecode = new byte[bufferSize];
            var memoryStream = new MemoryStream(bytecode);
            var writer = new BinaryWriter(memoryStream);

            foreach (var instruction in AllInstructions())
            {
                //instruction.Print();
                instruction.Write(writer);
            }

            return bytecode;
        }

        private DebugInfo GenerateDebugInfo()
        {
            if (!Options.GenerateDebugInfo)
                return null;

            var prevName = -1;

            var functions = AllInstructions()
                .Where(i => i.Type == InstructionType.Function)
                .Select(i =>
                {
                    var name = ((ConstantOperand<string>)i.Operands[0]).Id;
                    return new DebugInfo.Function(i.Offset, name);
                })
                .Where(f =>
                {
                    if (f.Name == prevName)
                        return false;

                    prevName = f.Name;

                    return true;
                })
                .OrderBy(f => f.Address)
                .ToList();

            var prevFileName = -1;
            var prevLineNumber = -1;

            var lines = AllInstructions()
                 .Where(i => i.Type == InstructionType.Line)
                 .Select(i =>
                 {
                     var fileName = ((ConstantOperand<string>)i.Operands[0]).Id;
                     var line = ((ImmediateOperand)i.Operands[1]).Value;
                     return new DebugInfo.Line(i.Offset, fileName, line);
                 })
                 .Where(l =>
                 {
                     if (l.FileName == prevFileName && l.LineNumber == prevLineNumber)
                         return false;

                     prevFileName = l.FileName;
                     prevLineNumber = l.LineNumber;

                     return true;
                 })
                 .OrderBy(l => l.Address)
                 .ToList();

            return new DebugInfo(functions, lines);
        }

        private IEnumerable<Instruction> AllInstructions()
        {
            return _instructions ?? (_instructions = _contexts.SelectMany(c => c.Instructions).ToList());
        }

        public void RegisterFunction(FunctionContext context)
        {
            _contexts.Add(context);
        }

        public LabelOperand MakeLabel(string name = null)
        {
            return new LabelOperand(_labelIndex++, name);
        }

        public IdentifierOperand Identifier(string name)
        {
            return _scope.Get(name);
        }
    }
}
