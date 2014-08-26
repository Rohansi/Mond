using System;
using System.Collections.Generic;
using System.Linq;
using Mond.Compiler;
using Mond.Compiler.Expressions;
using Mond.VirtualMachine;

namespace Mond
{
    public sealed class MondProgram
    {
        internal readonly byte[] Bytecode;
        internal readonly List<MondValue> Numbers;
        internal readonly List<MondValue> Strings;
        internal readonly DebugInfo DebugInfo;

        internal MondProgram(byte[] bytecode, IEnumerable<double> numbers, IEnumerable<string> strings, DebugInfo debugInfo = null)
        {
            Bytecode = bytecode;
            Numbers = numbers.Select(n => new MondValue(n)).ToList();
            Strings = strings.Select(s => new MondValue(s)).ToList();
            DebugInfo = debugInfo;
        }

        /// <summary>
        /// Compile a Mond program from a string.
        /// </summary>
        /// <param name="source">Source code to compile</param>
        /// <param name="fileName">Optional file name to use in errors</param>
        /// <param name="options">Compiler options</param>
        public static MondProgram Compile(string source, string fileName = null, MondCompilerOptions options = null)
        {
            var lexer = new Lexer(source, fileName);
            return ParseAndCompile(lexer, options);
        }

        /// <summary>
        /// Compile a Mond program from a stream of characters.
        /// </summary>
        /// <param name="source">Source code to compile</param>
        /// <param name="fileName">Optional file name to use in errors</param>
        /// <param name="options">Compiler options</param>
        public static MondProgram Compile(IEnumerable<char> source, string fileName = null, MondCompilerOptions options = null)
        {
            var lexer = new Lexer(source, int.MaxValue, fileName);
            return ParseAndCompile(lexer, options);
        }

        /// <summary>
        /// Compiles a single statement from a stream of characters. This should
        /// only really be useful when implementing REPLs.
        /// </summary>
        /// <param name="source">Source code to compile</param>
        /// <param name="fileName">Optional file name to use in errors</param>
        /// <param name="options">Compiler options</param>
        public static MondProgram CompileStatement(IEnumerable<char> source, string fileName = null, MondCompilerOptions options = null)
        {
            var lexer = new Lexer(source, int.MaxValue, fileName);
            var parser = new Parser(lexer);
            var expression = new BlockExpression(new List<Expression>
            {
                parser.ParseStatement()
            });

            return CompileImpl(expression, options);
        }

        private static MondProgram ParseAndCompile(IEnumerable<Token> lexer, MondCompilerOptions options)
        {
            var parser = new Parser(lexer);
            var expression = parser.ParseAll();
            return CompileImpl(expression, options);
        }

        private static MondProgram CompileImpl(Expression expression, MondCompilerOptions options)
        {
            options = options ?? new MondCompilerOptions();

            expression.SetParent(null);
            expression.Simplify();

            //using (var writer = new IndentTextWriter(Console.Out, " "))
            //    expression.Print(writer);

            var compiler = new ExpressionCompiler(options);
            return compiler.Compile(expression);
        }
    }
}
