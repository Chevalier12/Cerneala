using Cerneala.Language.Diagnostics;
using Cerneala.Language.Semantics;
using Cerneala.Language.Timbre;

namespace Cerneala.SourceGen;

internal sealed class SourceGeneratorSemanticModel
{
    private SourceGeneratorSemanticModel(
        IReadOnlyList<CernealaSemanticSymbol> symbols,
        IReadOnlyList<LanguageDiagnostic> diagnostics,
        SoundMarkupModel sound)
    {
        Symbols = symbols;
        Diagnostics = diagnostics;
        Sound = sound;
    }

    public IReadOnlyList<CernealaSemanticSymbol> Symbols { get; }

    public IReadOnlyList<LanguageDiagnostic> Diagnostics { get; }

    // Bound SoundClip and Aspect Sound actions; SourceGen lowers them and
    // never re-validates Sound markup.
    public SoundMarkupModel Sound { get; }

    public static SourceGeneratorSemanticModel Create(CernealaSemanticModel model) => new(
        model.Symbols.ToArray(),
        model.Diagnostics.ToArray(),
        model.Sound);
}
