using System;
using System.Collections.Generic;
using Mond.Compiler;
using Mond.Compiler.Expressions;
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
        /// <param name="options"></param>
        public static MondProgram Compile(string source, string fileName = null, MondCompilerOptions options = null)
        {
            options = options ?? new MondCompilerOptions();

            var lexer = new Lexer(source, fileName);
            var parser = new Parser(lexer);
            var expression = parser.ParseAll();

            return CompileImpl(expression, options);
        }

        /// <summary>
        /// Compiles a single statement from a stream of characters. This should
        /// only really be useful when implementing REPLs.
        /// </summary>
        public static MondProgram CompileStatement(IEnumerable<char> source, string fileName = null, MondCompilerOptions options = null)
        {
            options = options ?? new MondCompilerOptions();

            var lexer = new Lexer(source, int.MaxValue, fileName);
            var parser = new Parser(lexer);
            var expression = parser.ParseStatement();

            return CompileImpl(expression, options);
        }

        private static MondProgram CompileImpl(Expression expression, MondCompilerOptions options)
        {
            expression.SetParent(null);
            expression.Simplify();

            //using (var writer = new IndentTextWriter(Console.Out, " "))
            //    expression.Print(writer);

            var compiler = new ExpressionCompiler(options);
            return compiler.Compile(expression);
        }
    }
}
