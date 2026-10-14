using Cerneala.Language.Diagnostics;
using Cerneala.Language.Semantics;
using Cerneala.Language.Timbre;

namespace Cerneala.SourceGen;

internal sealed class SourceGeneratorSemanticModel
{
    private SourceGeneratorSemanticModel(
        IReadOnlyList<CernealaSemanticSymbol> symbols,
        IReadOnlyList<LanguageDiagnostic> diagnostics,
        TimbreMarkupModel sound)
    {
        Symbols = symbols;
        Diagnostics = diagnostics;
        Timbre = sound;
    }

    public IReadOnlyList<CernealaSemanticSymbol> Symbols { get; }

    public IReadOnlyList<LanguageDiagnostic> Diagnostics { get; }

    // Bound TimbreClip and Aspect Timbre actions; SourceGen lowers them and
    // never re-validates Timbre markup.
    public TimbreMarkupModel Timbre { get; }

    // The incremental driver caches this model across compilations. Language
    // type and member symbols wrap the Roslyn compilation they were bound in,
    // and emission never reads them, so keep only compilation-independent data.
    public static SourceGeneratorSemanticModel Create(CernealaSemanticModel model) => new(
        model.Symbols.Select(symbol => new CernealaSemanticSymbol(
            symbol.Kind,
            symbol.Name,
            symbol.ValueType,
            symbol.Span,
            value: symbol.Value,
            contentPropertyName: symbol.ContentPropertyName,
            definitionLocation: symbol.DefinitionLocation,
            isWritable: symbol.IsWritable)).ToArray(),
        model.Diagnostics.ToArray(),
        model.Timbre);
}
