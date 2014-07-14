using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mond.Compiler.Expressions;
using Mond.VirtualMachine;

namespace Mond.Compiler
{
    class ExpressionCompiler
    {
        private readonly List<CompilerContext> _contexts;
        private Scope _currentScope;
        private int _labelIndex;

        public int FrameIndex { get; private set; }

        public readonly bool GeneratingDebugInfo;

        public readonly ConstantPool<double> NumberPool;
        public readonly ConstantPool<string> StringPool;
         
        public ExpressionCompiler(bool generateDebugInfo = true)
        {
            _contexts = new List<CompilerContext>();
            _currentScope = null;
            _labelIndex = 0;

            FrameIndex = -1;

            GeneratingDebugInfo = generateDebugInfo;

            NumberPool = new ConstantPool<double>();
            StringPool = new ConstantPool<string>();
        }

        public MondProgram Compile(Expression expression)
        {
            PushFrame();

            var context = MakeContext();
            context.Function(expression.FileName, "#main");

            context.Enter();
            expression.Compile(context);
            context.LoadUndefined();
            context.Return();

            PopFrame();

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
            if (!GeneratingDebugInfo)
                return null;

            var prevName = -1;
            var prevFileName = -1;

            var functions = AllInstructions()
                .Where(i => i.Type == InstructionType.Function)
                .Select(i =>
                {
                    var name = ((ConstantOperand<string>)i.Operands[0]).Id;
                    var fileName = ((ConstantOperand<string>)i.Operands[1]).Id;
                    return new DebugInfo.Function(i.Offset, name, fileName);
                })
                .Where(f =>
                {
                    if (f.Name == prevName && f.FileName == prevFileName)
                        return false;

                    prevName = f.Name;
                    prevFileName = f.FileName;

                    return true;
                })
                .OrderBy(f => f.Address)
                .ToList();

            prevFileName = -1;
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
            return _contexts.SelectMany(c => c.Instructions);
        }

        public CompilerContext MakeContext()
        {
            var context = new CompilerContext(this);
            _contexts.Add(context);
            return context;
        }

        public void PushScope()
        {
            _currentScope = new Scope(FrameIndex, _currentScope);
        }

        public void PopScope()
        {
            _currentScope = _currentScope.Previous;
        }

        public void PushFrame()
        {
            FrameIndex++;
            PushScope();
        }

        public void PopFrame()
        {
            PopScope();
            FrameIndex--;
        }

        public IdentifierOperand Identifier(string name)
        {
            return _currentScope.Get(name);
        }

        public LabelOperand Label(string name = null)
        {
            return new LabelOperand(_labelIndex++, name);
        }

        public bool DefineIdentifier(string name, bool isReadOnly)
        {
            return _currentScope.Define(name, isReadOnly);
        }

        public bool DefineArgument(int index, string name)
        {
            return _currentScope.DefineArgument(index, name);
        }
    }
}
