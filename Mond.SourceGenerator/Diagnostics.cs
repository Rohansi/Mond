using Microsoft.CodeAnalysis;

namespace Mond.SourceGenerator;

internal static class Diagnostics
{
    private const string Category = "Mond";

    public static readonly DiagnosticDescriptor MissingSyntaxReceiver = new DiagnosticDescriptor(
        "MOND00",
        "Internal error - syntax receiver is null",
        "The syntax receiver was not set or is an unexpected type - cannot generate Mond bindings.", Category,
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MondValueNotFound = new DiagnosticDescriptor(
        "MOND01",
        "MondValue type symbol was not found",
        "The MondValue type symbol was not found. Are you missing an assembly reference to Mond?", Category,
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MondStateNotFound = new DiagnosticDescriptor(
        "MOND02",
        "MondState type symbol was not found",
        "The MondState type symbol was not found. Are you missing an assembly reference to Mond?", Category,
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor CannotBindGeneric = new DiagnosticDescriptor(
        "MOND03",
        "Cannot generate Mond bindings for generic types",
        "Open generic types cannot be bound to Mond. Either remove the generic parameters from this or bind closed types which derive from this type instead.", Category,
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor ClassesCannotBeStatic = new DiagnosticDescriptor(
        "MOND04",
        "Static classes cannot be bound with MondClassAttribute",
        "MondClassAttribute is only meant for classes that can be instantiated. Either switch to MondModuleAttribute or remove the static modifier.", Category,
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor BoundClassesMustBePartial = new DiagnosticDescriptor(
        "MOND05",
        "Bound classes and all parent types must have the partial modifier",
        "Bound classes and all parent types must have the partial modifier to support source generation.", Category,
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor BoundMembersMustBePublic = new DiagnosticDescriptor(
        "MOND06",
        "Bound members must be public",
        "Bound class members must have public visibility.", Category,
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor BoundMethodsCannotBeFunctionAndOperator = new DiagnosticDescriptor(
        "MOND07",
        "Bound methods cannot have both MondFunctionAttribute and MondOperatorAttribute",
        "Methods cannot have both the MondFunctionAttribute and MondOperatorAttribute attributes applied.", Category,
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor UnsupportedParameterType = new DiagnosticDescriptor(
        "MOND08",
        "Method parameter type is not supported",
        "The method parameter type `{0}` is not supported in Mond bindings.", Category,
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor CannotConvertToMondValue = new DiagnosticDescriptor(
        "MOND09",
        "Cannot convert type to MondValue",
        "The type `{0}` cannot be automatically converted to a MondValue in generated bindings.", Category,
        DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor CannotConvertFromMondValue = new DiagnosticDescriptor(
        "MOND10",
        "Cannot convert type from MondValue",
        "The type `{0}` cannot be automatically converted from a MondValue in generated bindings.", Category,
        DiagnosticSeverity.Error, true);
}
