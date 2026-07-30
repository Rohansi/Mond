using System;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Running;

namespace Mond.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        // `dotnet run -c Release -- --smoke` runs every benchmark once, which is a
        // quick way to check the scripts still work after changing the VM.
        if (args.Contains("--smoke"))
        {
            Smoke();
            return;
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }

    private static void Smoke()
    {
        var vm = new VirtualMachineBenchmarks();
        foreach (var script in Scripts.Names)
        {
            vm.Script = script;
            vm.Setup();

            var sw = Stopwatch.StartNew();
            var result = vm.Run();
            sw.Stop();

            Console.WriteLine($"{script,-14} {sw.Elapsed.TotalMilliseconds,8:F2} ms  => {result}");
        }

        var compiler = new CompilerBenchmarks { Script = Scripts.Names.First() };
        compiler.Setup();
        compiler.Compile();
        compiler.CompileWithDebugInfo();
        Console.WriteLine("compiler       ok");

        var interop = new InteropBenchmarks();
        interop.Setup();
        Console.WriteLine($"interop        ok => {interop.CallMondFunction()}, {interop.CallNativeFunction()}");
    }
}
