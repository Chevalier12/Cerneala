using System.Collections.ObjectModel;

namespace Cerneala.UI.Markup;

public sealed class MarkupResult<T>
{
    public MarkupResult(T? value, IEnumerable<MarkupDiagnostic>? diagnostics = null)
    {
        Value = value;
        Diagnostics = new ReadOnlyCollection<MarkupDiagnostic>((diagnostics ?? []).ToList());
    }

    public T? Value { get; }

    public IReadOnlyList<MarkupDiagnostic> Diagnostics { get; }

    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == MarkupDiagnosticSeverity.Error);
}
