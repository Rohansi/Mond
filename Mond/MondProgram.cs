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
        /// Compile a Mond program from a stream of characters.
        /// </summary>
        /// <param name="source">Source code to compile</param>
        /// <param name="fileName">Optional file name to use in errors</param>
        /// <param name="options">Compiler options</param>
        public static MondProgram Compile(IEnumerable<char> source, string fileName = null, MondCompilerOptions options = null)
        {
            var lexer = new Lexer(source, fileName);
            var parser = new Parser(lexer);
            return CompileImpl(parser.ParseAll(), options);
        }

        /// <summary>
        /// Compiles statements from an infinite stream of characters.
        /// This should only be useful when implementing REPLs.
        /// </summary>
        /// <param name="source">Source code to compile</param>
        /// <param name="fileName">Optional file name to use in errors</param>
        /// <param name="options">Compiler options</param>
        /// <param name="errorHandler">Callback to call if an exception is thrown</param>
        public static IEnumerable<MondProgram> CompileStatements(IEnumerable<char> source, string fileName = null, MondCompilerOptions options = null, Action<Exception> errorHandler = null)
        {
            var lexer = new Lexer(source, fileName);
            var parser = new Parser(lexer);

            while (true)
            {
                MondProgram program;

                try
                {
                    var expression = new BlockExpression(new[]
                    {
                        parser.ParseStatement()
                    });

                    program = CompileImpl(expression, options);
                }
                catch (Exception e)
                {
                    if (errorHandler == null)
                        throw;

                    errorHandler(e);
                    continue;
                }

                yield return program;
            }
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
