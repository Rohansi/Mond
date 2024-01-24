using System.Linq;
using Microsoft.CodeAnalysis;

namespace Mond.SourceGenerator
{
    [Generator]
    public class MondSourceGenerator : ISourceGenerator
    {
        private const string Category = "Mond";

        private static readonly DiagnosticDescriptor MissingSyntaxReceiver = new DiagnosticDescriptor(
            "MOND001",
            "Internal error - syntax receiver is null",
            "The syntax receiver was not set or is an unexpected type - cannot generate Mond bindings.", Category,
            DiagnosticSeverity.Error, true);

        private static readonly DiagnosticDescriptor CannotBindGeneric = new DiagnosticDescriptor(
            "MOND002",
            "Cannot generate Mond bindings for generic types",
            "Open generic types cannot be bound to Mond. Either remove the generic parameters from this or bind closed types which derive from this type instead.", Category,
            DiagnosticSeverity.Error, true);

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxContextReceiver is SyntaxReceiver syntaxReceiver))
            {
                context.ReportDiagnostic(Diagnostic.Create(MissingSyntaxReceiver, Location.None));
                return;
            }

            // https://stackoverflow.com/questions/64623689/get-all-types-from-compilation-using-roslyn
            context.Compilation.GlobalNamespace.GetMembers();

            foreach (var module in syntaxReceiver.Modules)
            {
                if (module.Arity != 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(CannotBindGeneric, module.Locations.First()));
                    continue;
                }
            }

            foreach (var klass in syntaxReceiver.Classes)
            {
                if (klass.Arity != 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(CannotBindGeneric, klass.Locations.First()));
                    continue;
                }
            }
        }
    }
}
