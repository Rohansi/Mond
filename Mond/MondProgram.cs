using System.Collections.Generic;
using Mond.Compiler;
using Mond.VirtualMachine;

namespace Mond
{
    public sealed class MondProgram
    {
        internal readonly byte[] Bytecode;
        internal readonly List<double> Numbers;
        internal readonly List<string> Strings;
        internal readonly DebugInfo DebugInfo;

        internal MondProgram(byte[] bytecode, List<double> numbers, List<string> strings, DebugInfo debugInfo = null)
        {
            Bytecode = bytecode;
            Numbers = numbers;
            Strings = strings;
            DebugInfo = debugInfo;
        }

        /// <summary>
        /// Compile a Mond program from a string.
        /// </summary>
        /// <param name="source">Source code to compile</param>
        /// <param name="fileName">Optional file name to use in errors</param>
        /// <param name="generateDebugInfo">Enable generating debug info</param>
        public static MondProgram Compile(string source, string fileName = null, bool generateDebugInfo = true)
        {
            var lexer = new Lexer(source, fileName);
            var parser = new Parser(lexer);
            var expression = parser.ParseAll();
            expression.SetParent(null);
            expression.Simplify();
            expression.Print(0);

            var compiler = new ExpressionCompiler(generateDebugInfo);
            return compiler.Compile(expression);
        }
    }
}
