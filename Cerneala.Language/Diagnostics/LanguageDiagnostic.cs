using System.Globalization;
using Cerneala.Language.Text;

namespace Cerneala.Language.Diagnostics;

internal sealed class LanguageDiagnostic
{
    public LanguageDiagnostic(
        LanguageDiagnosticDescriptor descriptor,
        TextSpan span,
        AnalysisMode mode,
        params object[] arguments)
    {
        Descriptor = descriptor;
        Span = span;
        Severity = descriptor.GetSeverity(mode);
        Arguments = arguments ?? Array.Empty<object>();
        Message = string.Format(CultureInfo.InvariantCulture, descriptor.MessageFormat, arguments ?? Array.Empty<object>());
    }

    public LanguageDiagnosticDescriptor Descriptor { get; }

    public string Id => Descriptor.Id;

    public TextSpan Span { get; }

    public LanguageDiagnosticSeverity Severity { get; }

    public IReadOnlyList<object> Arguments { get; }

    public string Message { get; }
}
