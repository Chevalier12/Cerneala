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

    public static SourceGeneratorSemanticModel Create(CernealaSemanticModel model) => new(
        model.Symbols.ToArray(),
        model.Diagnostics.ToArray(),
        model.Timbre);
}
