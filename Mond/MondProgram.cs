using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Mond.Compiler;
using Mond.Compiler.Expressions;
using Mond.Debugger;

namespace Mond
{
    public sealed class MondProgram
    {
        private const uint MagicId = 0xFA57C0DE;
        private const byte FormatVersion = 8;

        internal readonly byte[] Bytecode;
        internal readonly MondValue[] Numbers;
        internal readonly MondValue[] Strings;
        public MondDebugInfo DebugInfo { get; }

        internal MondProgram(byte[] bytecode, IList<double> numbers, IList<string> strings, MondDebugInfo debugInfo = null)
        {
            Bytecode = bytecode;

            Numbers = new MondValue[numbers.Count];
            for (var i = 0; i < Numbers.Length; i++)
            {
                Numbers[i] = MondValue.Number(numbers[i]);
            }

            Strings = new MondValue[strings.Count];
            for (var i = 0; i < Strings.Length; i++)
            {
                Strings[i] = MondValue.String(strings[i]);
            }
            
            DebugInfo = debugInfo;
        }

        /// <summary>
        /// Writes the compiled Mond program to the specified file.
        /// </summary>
        /// <param name="path">File to write the bytecode to.</param>
        public void SaveBytecode(string path)
        {
            using (var fs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                SaveBytecode(fs);
            }
        }

        /// <summary>
        /// Writes the compiled Mond program to the specified stream.
        /// </summary>
        /// <param name="output">Stream to write to.</param>
        public void SaveBytecode(Stream output)
        {
            var writer = new BinaryWriter(output, Encoding.UTF8);

            writer.Write(MagicId);
            writer.Write(FormatVersion);
            writer.Write(DebugInfo != null);

            writer.Write(Strings.Length);
            foreach (var str in Strings)
            {
                var buf = Encoding.UTF8.GetBytes(str.ToString());
                writer.Write(buf.Length);
                writer.Write(buf);
            }

            writer.Write(Numbers.Length);
            foreach (var num in Numbers)
            {
                writer.Write((double)num);
            }

            writer.Write(Bytecode.Length);
            writer.Write(Bytecode, 0, Bytecode.Length);

            if (DebugInfo != null)
            {
                writer.Write(DebugInfo.FileName ?? "");
                writer.Write(DebugInfo.SourceCode ?? "");

                if (DebugInfo.Functions != null)
                {
                    writer.Write(DebugInfo.Functions.Count);
                    foreach (var function in DebugInfo.Functions)
                    {
                        writer.Write(function.Address);
                        writer.Write(function.Name);
                    }
                }
                else
                {
                    writer.Write(-1);
                }

                if (DebugInfo.Lines != null)
                {
                    writer.Write(DebugInfo.Lines.Count);
                    foreach (var line in DebugInfo.Lines)
                    {
                        writer.Write(line.Address);
                        writer.Write(line.LineNumber);
                        writer.Write(line.ColumnNumber);
                    }
                }
                else
                {
                    writer.Write(-1);
                }

                if (DebugInfo.Statements != null)
                {
                    writer.Write(DebugInfo.Statements.Count);
                    foreach (var statement in DebugInfo.Statements)
                    {
                        writer.Write(statement.Address);
                        writer.Write(statement.StartLineNumber);
                        writer.Write(statement.StartColumnNumber);
                        writer.Write(statement.EndLineNumber);
                        writer.Write(statement.EndColumnNumber);
                    }
                }
                else
                {
                    writer.Write(-1);
                }

                if (DebugInfo.Scopes != null)
                {
                    writer.Write(DebugInfo.Scopes.Count);
                    foreach (var scope in DebugInfo.Scopes)
                    {
                        writer.Write(scope.Id);
                        writer.Write(scope.Depth);
                        writer.Write(scope.ParentId);
                        writer.Write(scope.StartAddress);
                        writer.Write(scope.EndAddress);

                        writer.Write(scope.Identifiers.Count);
                        foreach (var ident in scope.Identifiers)
                        {
                            writer.Write(ident.Name);
                            writer.Write(ident.IsReadOnly);
                            writer.Write(ident.FrameIndex);
                            writer.Write(ident.Id);
                        }
                    }
                }
                else
                {
                    writer.Write(-1);
                }

                writer.Flush();
            }
        }

        /// <summary>
        /// Load a Mond source code from a file and return the compiled program.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <param name="options">Compiler options.</param>
        public static MondProgram FromFile(string path, MondCompilerOptions options = null)
        {
            var source = File.ReadAllText(path, Encoding.UTF8);
            var name = Path.GetFileName(path);
            return Compile(source, name, options);
        }

        /// <summary>
        /// Loads Mond bytecode from the specified file.
        /// </summary>
        /// <param name="path">The file to load.</param>
        public static MondProgram LoadBytecode(string path)
        {
            using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return LoadBytecode(fs);
            }
        }

        /// <summary>
        /// Loads Mond bytecode from the specified stream.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        public static MondProgram LoadBytecode(Stream stream)
        {
            var reader = new BinaryReader(stream, Encoding.UTF8);

            if (reader.ReadUInt32() != MagicId)
                throw new NotSupportedException("Stream data is not valid Mond bytecode.");

            byte version;
            if ((version = reader.ReadByte()) != FormatVersion)
                throw new NotSupportedException(string.Format("Wrong bytecode version. Expected 0x{0:X2}, got 0x{1:X2}.", FormatVersion, version));

            var hasDebugInfo = reader.ReadBoolean();

            var stringCount = reader.ReadInt32();
            var strings = new List<string>(stringCount);
            for (var i = 0; i < stringCount; ++i)
            {
                var len = reader.ReadInt32();
                var buf = reader.ReadBytes(len);
                var str = Encoding.UTF8.GetString(buf);
                strings.Add(str);
            }

            var numberCount = reader.ReadInt32();
            var numbers = new List<double>(numberCount);
            for (var i = 0; i < numberCount; ++i)
            {
                numbers.Add(reader.ReadDouble());
            }

            var bytecodeLength = reader.ReadInt32();
            var bytecode = reader.ReadBytes(bytecodeLength);

            MondDebugInfo debugInfo = null;
            if (hasDebugInfo)
            {
                var fileName = reader.ReadString();
                var sourceCode = reader.ReadString();

                var functionCount = reader.ReadInt32();
                List<MondDebugInfo.Function> functions = null;

                if (functionCount >= 0)
                {
                    functions = new List<MondDebugInfo.Function>(functionCount);
                    for (var i = 0; i < functionCount; ++i)
                    {
                        var address = reader.ReadInt32();
                        var name = reader.ReadInt32();
                        var function = new MondDebugInfo.Function(address, name);
                        functions.Add(function);
                    }
                }

                var lineCount = reader.ReadInt32();
                List<MondDebugInfo.Position> lines = null;

                if (lineCount >= 0)
                {
                    lines = new List<MondDebugInfo.Position>(lineCount);
                    for (var i = 0; i < lineCount; ++i)
                    {
                        var address = reader.ReadInt32();
                        var lineNumber = reader.ReadInt32();
                        var columnNumber = reader.ReadInt32();

                        var line = new MondDebugInfo.Position(address, lineNumber, columnNumber);
                        lines.Add(line);
                    }
                }

                var statementCount = reader.ReadInt32();
                List<MondDebugInfo.Statement> statements = null;

                if (statementCount >= 0)
                {
                    statements = new List<MondDebugInfo.Statement>(statementCount);
                    for (var i = 0; i < statementCount; ++i)
                    {
                        var address = reader.ReadInt32();
                        var startLine = reader.ReadInt32();
                        var startColumn = reader.ReadInt32();
                        var endLine = reader.ReadInt32();
                        var endColumn = reader.ReadInt32();

                        var statement = new MondDebugInfo.Statement(address, startLine, startColumn, endLine, endColumn);
                        statements.Add(statement);
                    }
                }

                var scopeCount = reader.ReadInt32();
                List<MondDebugInfo.Scope> scopes = null;

                if (scopeCount >= 0)
                {
                    scopes = new List<MondDebugInfo.Scope>(scopeCount);
                    for (var i = 0; i < scopeCount; ++i)
                    {
                        var id = reader.ReadInt32();
                        var depth = reader.ReadInt32();
                        var parentId = reader.ReadInt32();
                        var startAddress = reader.ReadInt32();
                        var endAddress = reader.ReadInt32();

                        var identCount = reader.ReadInt32();
                        var idents = new List<MondDebugInfo.Identifier>(identCount);
                        for (var j = 0; j < identCount; ++j)
                        {
                            var name = reader.ReadInt32();
                            var isReadOnly = reader.ReadBoolean();
                            var frameIndex = reader.ReadInt32();
                            var idx = reader.ReadInt32();

                            idents.Add(new MondDebugInfo.Identifier(name, isReadOnly, frameIndex, idx));
                        }

                        scopes.Add(new MondDebugInfo.Scope(id, depth, parentId, startAddress, endAddress, idents));
                    }
                }

                debugInfo = new MondDebugInfo(fileName, sourceCode, functions, lines, statements, scopes);
            }

            return new MondProgram(bytecode, numbers, strings, debugInfo);
        }

        /// <summary>
        /// Compile a Mond program from a string.
        /// </summary>
        /// <param name="source">Source code to compile</param>
        /// <param name="fileName">Optional file name to use in errors</param>
        /// <param name="options">Compiler options</param>
        public static MondProgram Compile(string source, string fileName = null, MondCompilerOptions options = null)
        {
            options = options ?? new MondCompilerOptions();

            var lexer = new Lexer(source, fileName, options);
            var parser = new Parser(lexer);

            return CompileImpl(parser.ParseAll(), options, source);
        }

        /// <summary>
        /// Compile a Mond program from a stream of characters.
        /// </summary>
        /// <param name="source">Source code to compile</param>
        /// <param name="fileName">Optional file name to use in errors</param>
        /// <param name="options">Compiler options</param>
        public static MondProgram Compile(IEnumerable<char> source, string fileName = null, MondCompilerOptions options = null)
        {
            options = options ?? new MondCompilerOptions();

            var needSource = options.DebugInfo == MondDebugInfoLevel.Full;
            var lexer = new Lexer(source, fileName, options, needSource);
            var parser = new Parser(lexer);

            return CompileImpl(parser.ParseAll(), options, lexer.SourceCode);
        }

        /// <summary>
        /// Compiles statements from an infinite stream of characters.
        /// This should only be useful when implementing REPLs.
        /// </summary>
        /// <param name="source">Source code to compile</param>
        /// <param name="fileName">Optional file name to use in errors</param>
        /// <param name="options">Compiler options</param>
        public static IEnumerable<MondProgram> CompileStatements(IEnumerable<char> source, string fileName = null, MondCompilerOptions options = null)
        {
            options = options ?? new MondCompilerOptions();

            var needSource = options.DebugInfo == MondDebugInfoLevel.Full;
            var lexer = new Lexer(source, fileName, options, needSource);
            var parser = new Parser(lexer);

            while (true)
            {
                var expression = new BlockExpression(new[]
                {
                    parser.ParseStatement()
                });

                var sourceCode = lexer.SourceCode?.TrimStart('\r', '\n');
                yield return CompileImpl(expression, options, sourceCode);
            }
        }

        private static MondProgram CompileImpl(Expression expression, MondCompilerOptions options, string debugSourceCode = null)
        {
            expression = expression.Simplify();
            expression.SetParent(null);

            //using (var printer = new ExpressionPrintVisitor(Console.Out))
            //    expression.Accept(printer);

            var compiler = new ExpressionCompiler(options);
            return compiler.Compile(expression, debugSourceCode);
        }
    }
}
