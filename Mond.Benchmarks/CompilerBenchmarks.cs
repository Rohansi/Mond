using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace Mond.Benchmarks;

/// <summary>
/// Measures the lexer, parser and code generator.
/// </summary>
[MemoryDiagnoser]
public class CompilerBenchmarks
{
    private string _source;

    public static IEnumerable<string> ScriptNames => Scripts.Names;

    [ParamsSource(nameof(ScriptNames))]
    public string Script { get; set; }

    [GlobalSetup]
    public void Setup() => _source = Scripts.GetSource(Script);

    [Benchmark]
    public MondProgram Compile() => MondProgram.Compile(_source, Script);

    [Benchmark]
    public MondProgram CompileWithDebugInfo() => MondProgram.Compile(_source, Script,
        new MondCompilerOptions { DebugInfo = MondDebugInfoLevel.Full });
}
