using Microsoft.CodeAnalysis;

namespace Mond.SourceGenerator;

internal static class Diagnostics
{
    private const string Category = "Mond";

    public static readonly DiagnosticDescriptor MissingSyntaxReceiver = new DiagnosticDescriptor(
        "MOND000",
        "Internal error - syntax receiver is null",
        "The syntax receiver was not set or is an unexpected type - cannot generate Mond bindings.", Category,
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MondValueNotFound = new DiagnosticDescriptor(
        "MOND001",
        "MondValue type symbol was not found",
        "The MondValue type symbol was not found. Are you missing an assembly reference to Mond?", Category,
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MondStateNotFound = new DiagnosticDescriptor(
        "MOND002",
        "MondState type symbol was not found",
        "The MondState type symbol was not found. Are you missing an assembly reference to Mond?", Category,
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor CannotBindGeneric = new DiagnosticDescriptor(
        "MOND003",
        "Cannot generate Mond bindings for generic types",
        "Open generic types cannot be bound to Mond. Either remove the generic parameters from this or bind closed types which derive from this type instead.", Category,
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor ClassesCannotBeStatic = new DiagnosticDescriptor(
        "MOND004",
        "Static classes cannot be bound with MondClassAttribute",
        "MondClassAttribute is only meant for classes that can be instantiated. Either switch to MondModuleAttribute or remove the static modifier.", Category,
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor BoundClassesMustBePartial = new DiagnosticDescriptor(
        "MOND005",
        "Bound classes and all parent types must have the partial modifier",
        "Bound classes and all parent types must have the partial modifier to support source generation.", Category,
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor BoundMembersMustBePublic = new DiagnosticDescriptor(
        "MOND006",
        "Bound members must be public",
        "Bound class members must have public visibility.", Category,
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor BoundMethodsCannotBeFunctionAndOperator = new DiagnosticDescriptor(
        "MOND007",
        "Bound methods cannot have both MondFunctionAttribute and MondOperatorAttribute",
        "Methods cannot have both the MondFunctionAttribute and MondOperatorAttribute attributes applied.", Category,
        DiagnosticSeverity.Error, true);
}
