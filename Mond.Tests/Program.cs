using System.Reflection;
using NUnitLite;

namespace Mond.Tests
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var assembly = typeof(Program).GetTypeInfo().Assembly;
            return new AutoRun(assembly).Execute(args);
        }
    }
}
