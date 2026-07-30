using BenchmarkDotNet.Attributes;

namespace Mond.Benchmarks;

/// <summary>
/// Measures the cost of crossing the boundary between C# and Mond in both directions.
/// </summary>
[MemoryDiagnoser]
public class InteropBenchmarks
{
    private const string CallFromCSharpSource = """
        return fun (a, b) -> a + b;
        """;

    private const string CallToNativeSource = """
        return fun () {
            var add = global.nativeAdd;
            var sum = 0;
            for (var i = 0; i < 10000; i++) {
                sum = add(sum, i);
            }
            return sum;
        };
        """;

    private MondState _state;
    private MondValue _add;
    private MondValue _nativeCaller;

    [GlobalSetup]
    public void Setup()
    {
        _state = new MondState();
        _state["nativeAdd"] = MondValue.Function(static (_, args) => args[0] + args[1]);

        _add = _state.Load(MondProgram.Compile(CallFromCSharpSource, "add.mnd"));
        _nativeCaller = _state.Load(MondProgram.Compile(CallToNativeSource, "nativeCaller.mnd"));

        _ = _state.Call(_add, 1, 2);
        _ = _state.Call(_nativeCaller);
    }

    [Benchmark(Description = "C# -> Mond function call")]
    public MondValue CallMondFunction() => _state.Call(_add, 1, 2);

    [Benchmark(Description = "Mond -> C# function calls (10k)")]
    public MondValue CallNativeFunction() => _state.Call(_nativeCaller);
}
