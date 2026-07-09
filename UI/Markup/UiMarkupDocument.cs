using System.Collections.ObjectModel;

namespace Cerneala.UI.Markup;

public sealed class UiMarkupDocument
{
    public UiMarkupDocument(UiMarkupNode? root, IEnumerable<MarkupDiagnostic>? diagnostics = null)
    {
        Root = root;
        Diagnostics = new ReadOnlyCollection<MarkupDiagnostic>((diagnostics ?? []).ToList());
    }

    public UiMarkupNode? Root { get; }

    public IReadOnlyList<MarkupDiagnostic> Diagnostics { get; }

    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == MarkupDiagnosticSeverity.Error);

    public static UiMarkupDocument FromRoot(UiMarkupNode root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return new UiMarkupDocument(root);
    }
}
