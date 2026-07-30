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

        // `dotnet run -c Release -- --profile <name> [iterations]` runs a single benchmark in a
        // plain loop so profiler traces only contain Mond code instead of BenchmarkDotNet's
        // host process, code generation and infrastructure.
        var profileIndex = Array.IndexOf(args, "--profile");
        if (profileIndex >= 0)
        {
            var name = profileIndex + 1 < args.Length ? args[profileIndex + 1] : null;
            var iterations = profileIndex + 2 < args.Length && int.TryParse(args[profileIndex + 2], out var parsed)
                ? parsed
                : 100;

            Profile(name, iterations);
            return;
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }

    private static void Profile(string name, int iterations)
    {
        if (string.IsNullOrEmpty(name))
        {
            PrintProfileUsage();
            return;
        }

        PinToProfilingCores();

        Action body;

        if (name.StartsWith("compile:", StringComparison.OrdinalIgnoreCase))
        {
            var script = ResolveScript(name["compile:".Length..]);
            if (script == null)
                return;

            var compiler = new CompilerBenchmarks { Script = script };
            compiler.Setup();
            body = () => compiler.Compile();
        }
        else if (name.Equals("interop", StringComparison.OrdinalIgnoreCase))
        {
            var interop = new InteropBenchmarks();
            interop.Setup();
            body = () =>
            {
                interop.CallMondFunction();
                interop.CallNativeFunction();
            };
        }
        else
        {
            var script = ResolveScript(name);
            if (script == null)
                return;

            var vm = new VirtualMachineBenchmarks { Script = script };
            vm.Setup();
            body = () => vm.Run();
        }

        Console.WriteLine($"profiling {name} for {iterations} iteration(s)...");

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            body();
        }
        sw.Stop();

        Console.WriteLine($"done in {sw.Elapsed.TotalMilliseconds:F2} ms " +
                          $"({sw.Elapsed.TotalMilliseconds / iterations:F3} ms/iteration)");
    }

    // keeping the process on a fixed set of cores stops the scheduler from migrating it between
    // them, which otherwise shows up as unexplained variance and split stacks in profiler traces
    private const int ProfilingCoreCount = 4;

    private static void PinToProfilingCores()
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
        {
            Console.WriteLine("processor affinity is not supported on this platform, running unpinned");
            return;
        }

        var coreCount = Math.Min(ProfilingCoreCount, Environment.ProcessorCount);
        var affinity = (1 << coreCount) - 1;

        try
        {
            using var process = Process.GetCurrentProcess();
            process.ProcessorAffinity = (IntPtr)affinity;
            Console.WriteLine($"pinned to cores 0-{coreCount - 1} (affinity mask 0x{affinity:X})");
        }
        catch (Exception e) when (e is PlatformNotSupportedException or NotSupportedException)
        {
            Console.WriteLine($"could not set processor affinity: {e.Message}");
        }
    }

    private static string ResolveScript(string name)
    {
        var script = Scripts.Names.FirstOrDefault(s => string.Equals(s, name, StringComparison.OrdinalIgnoreCase));
        if (script == null)
        {
            Console.WriteLine($"unknown script '{name}'");
            PrintProfileUsage();
        }

        return script;
    }

    private static void PrintProfileUsage()
    {
        Console.WriteLine("usage: --profile <name> [iterations]");
        Console.WriteLine("  <script>          run the script through the VM");
        Console.WriteLine("  compile:<script>  compile the script");
        Console.WriteLine("  interop           run the interop benchmarks");
        Console.WriteLine($"scripts: {string.Join(", ", Scripts.Names)}");
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
