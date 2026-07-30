# Mond.Benchmarks

[BenchmarkDotNet](https://benchmarkdotnet.org/) benchmarks for the Mond compiler and virtual machine.

## Running

Benchmarks must be run in Release:

```
dotnet run --project Mond.Benchmarks -c Release
```

Run a subset with a filter:

```
dotnet run --project Mond.Benchmarks -c Release -- --filter *VirtualMachine*
dotnet run --project Mond.Benchmarks -c Release -- --filter *Fields*
```

Quickly check that all benchmarks still work (no measurement, runs each once):

```
dotnet run --project Mond.Benchmarks -c Release -- --smoke
```

Run a single benchmark in a plain loop, without BenchmarkDotNet, so profiler traces are not
polluted by its host process and infrastructure:

```
dotnet run --project Mond.Benchmarks -c Release -- --profile Fields 200
dotnet run --project Mond.Benchmarks -c Release -- --profile compile:Fields 200
dotnet run --project Mond.Benchmarks -c Release -- --profile interop 200
```

The iteration count is optional and defaults to 100. Omitting the name prints the list of
available targets.

## Layout

| File | What it measures |
| --- | --- |
| `VirtualMachineBenchmarks.cs` | The interpreter loop. Scripts are compiled during setup, so only execution is timed. |
| `CompilerBenchmarks.cs` | Lexer, parser and code generator, with and without debug info. |
| `InteropBenchmarks.cs` | Cost of calling into Mond from C# and calling native functions from Mond. |

## Scripts

The VM and compiler benchmarks are driven by the `.mnd` files in `Scripts/`, which are embedded
into the assembly. Each script must evaluate to a **function taking no arguments** - that function
is what gets invoked per iteration:

```mond
return fun () {
	// work goes here
};
```

Each script targets a specific area of the VM:

| Script | Area |
| --- | --- |
| `Arithmetic.mnd` | Dispatch loop, `MondValue` operators, local slots |
| `Calls.mnd` | Call frame setup, argument passing, returns (recursive fib) |
| `Fields.mnd` | Object field get/set - `MondValue` hashing and dictionary lookups |
| `Arrays.mnd` | Array indexing fast paths |
| `MethodCalls.mnd` | `InstanceCall` and method lookup on objects |
| `Closures.mnd` | Closure allocation and upvalue access |
| `Sequences.mnd` | Sequence suspend/resume and `foreach` |
| `Globals.mnd` | Global object access |
| `Metamethods.mnd` | `TryDispatch` and the prototype chain walk |
| `Strings.mnd` | String concatenation and string prototype methods |

Adding a new scenario is just a matter of dropping another `.mnd` file into `Scripts/` - it will be
picked up automatically as a new benchmark parameter. Aim for roughly 5-20 ms per iteration.
