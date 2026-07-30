using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Mond.SourceGenerator;

internal sealed class TypeLookup
{
    public INamedTypeSymbol Void { get; private set; }
    public INamedTypeSymbol String { get; private set; }
    public INamedTypeSymbol Bool { get; private set; }
    public INamedTypeSymbol Task { get; private set; }
    public INamedTypeSymbol TaskOfT { get; private set; }
    public INamedTypeSymbol MondValue { get; private set; }
    public INamedTypeSymbol MondValueNullable { get; private set; }
    public INamedTypeSymbol MondValueSpan { get; private set; }
    public INamedTypeSymbol MondState { get; private set; }

    public Dictionary<ITypeSymbol, MondValueType[]> TypeCheckMap { get; private set; }
    public HashSet<ITypeSymbol> BasicTypes { get; private set; }
    public HashSet<ITypeSymbol> NumberTypes { get; private set; }

    private TypeLookup()
    {
    }

    public static bool TryCreate(SourceProductionContext context, Compilation compilation, out TypeLookup types)
    {
        types = null;

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
        var spanSym = compilation.GetTypeByMetadataName("System.Span`1");
        var taskSym = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        var taskOfTSym = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");

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

        var numberTypesArray = new[] { MondValueType.Number, MondValueType.Object };

        types = new TypeLookup
        {
            Void = voidSym,
            String = stringSym,
            Bool = boolSym,
            Task = taskSym,
            TaskOfT = taskOfTSym,
            MondValue = mondValueSym,
            MondValueNullable = nullableSym.Construct(mondValueSym),
            MondValueSpan = spanSym?.Construct(mondValueSym),
            MondState = mondStateSym,

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
            },

            // types with a direct conversion to/from MondValue
            BasicTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default)
            {
                doubleSym,
                stringSym,
                boolSym,
            },

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
            },
        };

        return true;
    }
}
