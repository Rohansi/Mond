using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Mond.Benchmarks;

/// <summary>
/// Loads the benchmark scripts that are embedded in this assembly.
/// Every script evaluates to a function taking no arguments, which is what
/// actually gets called by the benchmarks.
/// </summary>
public static class Scripts
{
    private const string Prefix = "Mond.Benchmarks.Scripts.";
    private const string Suffix = ".mnd";

    private static readonly Dictionary<string, string> Sources = Load();

    public static IEnumerable<string> Names => Sources.Keys.OrderBy(n => n);

    public static string GetSource(string name) => Sources[name];

    public static MondProgram Compile(string name) =>
        MondProgram.Compile(Sources[name], name + Suffix);

    private static Dictionary<string, string> Load()
    {
        var assembly = typeof(Scripts).Assembly;
        var result = new Dictionary<string, string>();

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(Prefix) || !resourceName.EndsWith(Suffix))
                continue;

            var name = resourceName[Prefix.Length..^Suffix.Length];
            result.Add(name, ReadResource(assembly, resourceName));
        }

        return result;
    }

    private static string ReadResource(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }
}
