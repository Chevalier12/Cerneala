using Cerneala.Language.Diagnostics;
using Cerneala.Language.Semantics;

namespace Cerneala.SourceGen;

internal sealed class SourceGeneratorSemanticModel
{
    private SourceGeneratorSemanticModel(
        IReadOnlyList<CernealaSemanticSymbol> symbols,
        IReadOnlyList<LanguageDiagnostic> diagnostics)
    {
        Symbols = symbols;
        Diagnostics = diagnostics;
    }

    public IReadOnlyList<CernealaSemanticSymbol> Symbols { get; }

    public IReadOnlyList<LanguageDiagnostic> Diagnostics { get; }

    public static SourceGeneratorSemanticModel Create(CernealaSemanticModel model) => new(
        model.Symbols.ToArray(),
        model.Diagnostics.ToArray());
}
