using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace Mond.Benchmarks;

/// <summary>
/// Runs each benchmark script through the virtual machine. Compilation happens
/// once during setup so only the interpreter is measured.
/// </summary>
[MemoryDiagnoser]
public class VirtualMachineBenchmarks
{
    private MondState _state;
    private MondValue _function;

    public static IEnumerable<string> ScriptNames => Scripts.Names;

    [ParamsSource(nameof(ScriptNames))]
    public string Script { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _state = new MondState();
        _function = _state.Load(Scripts.Compile(Script));

        if (_function.Type != MondValueType.Function)
            throw new InvalidOperationException($"Script '{Script}' must evaluate to a function");

        // make sure the script actually works before it gets measured
        _ = _state.Call(_function);
    }

    [Benchmark]
    public MondValue Run() => _state.Call(_function);
}
