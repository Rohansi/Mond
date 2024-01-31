using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Mond.SourceGenerator;

internal static class TypeLookup
{
    public static INamedTypeSymbol Void { get; private set; }
    public static INamedTypeSymbol String { get; private set; }
    public static INamedTypeSymbol Bool { get; private set; }
    public static INamedTypeSymbol MondValue { get; private set; }
    public static INamedTypeSymbol MondValueNullable { get; private set; }
    public static IArrayTypeSymbol MondValueArray { get; private set; }
    public static INamedTypeSymbol MondState { get; private set; }

    public static Dictionary<ITypeSymbol, MondValueType[]> TypeCheckMap { get; private set; }
    public static HashSet<ITypeSymbol> BasicTypes { get; private set; }
    public static HashSet<ITypeSymbol> NumberTypes { get; private set; }

    public static bool Initialize(GeneratorExecutionContext context)
    {
        var compilation = context.Compilation;

        var doubleSym = compilation.GetSpecialType(SpecialType.System_Double);
        var floatSym = compilation.GetSpecialType(SpecialType.System_Single);
        var intSym = compilation.GetSpecialType(SpecialType.System_Int32);
        var uintSym = compilation.GetSpecialType(SpecialType.System_UInt32);
        var shortSym = compilation.GetSpecialType(SpecialType.System_Int16);
        var ushortSym = compilation.GetSpecialType(SpecialType.System_UInt16);
        var sbyteSym = compilation.GetSpecialType(SpecialType.System_SByte);
        var byteSym = compilation.GetSpecialType(SpecialType.System_Byte);
        var voidSym = compilation.GetSpecialType(SpecialType.System_Void);
        var stringSym = compilation.GetSpecialType(SpecialType.System_String);
        var boolSym = compilation.GetSpecialType(SpecialType.System_Boolean);
        var nullableSym = compilation.GetSpecialType(SpecialType.System_Nullable_T);

        var mondValueSym = compilation.GetTypesByMetadataName("Mond.MondValue")
            .SingleOrDefault(s => s.ContainingAssembly.Identity.Name == "Mond");

        if (mondValueSym == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MondValueNotFound, Location.None));
            return false;
        }

        var mondStateSym = compilation.GetTypesByMetadataName("Mond.MondState")
            .SingleOrDefault(s => s.ContainingAssembly.Identity.Name == "Mond");

        if (mondStateSym == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MondStateNotFound, Location.None));
            return false;
        }

        Void = voidSym;
        String = stringSym;
        Bool = boolSym;
        MondValue = mondValueSym;
        MondValueNullable = nullableSym.Construct(mondValueSym);
        MondValueArray = compilation.CreateArrayTypeSymbol(mondValueSym);
        MondState = mondStateSym;

        var numberTypesArray = new[] { MondValueType.Number, MondValueType.Object };
        TypeCheckMap = new Dictionary<ITypeSymbol, MondValueType[]>(SymbolEqualityComparer.Default)
        {
            { doubleSym, numberTypesArray },
            { floatSym, numberTypesArray },
            { intSym, numberTypesArray },
            { uintSym, numberTypesArray },
            { shortSym, numberTypesArray },
            { ushortSym, numberTypesArray },
            { sbyteSym, numberTypesArray },
            { byteSym, numberTypesArray },
            { stringSym, [MondValueType.String, MondValueType.Object] },
            { boolSym, [MondValueType.True, MondValueType.False, MondValueType.Object] },
        };

        // types with a direct conversion to/from MondValue
        BasicTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default)
        {
            doubleSym,
            stringSym,
            boolSym,
        };

        // types that can be casted to/from double
        NumberTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default)
        {
            doubleSym,
            floatSym,
            intSym,
            uintSym,
            shortSym,
            ushortSym,
            sbyteSym,
            byteSym,
        };

        return true;
    }
}
