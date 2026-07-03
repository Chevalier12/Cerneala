namespace Cerneala.UI.Markup;

public enum MarkupDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public sealed record MarkupDiagnostic(
    MarkupDiagnosticSeverity Severity,
    string Code,
    string Message,
    int? Line = null,
    int? Column = null)
{
    public bool HasSourceLocation => Line is not null && Column is not null;

    public static MarkupDiagnostic Error(string code, string message, int? line = null, int? column = null)
    {
        return new MarkupDiagnostic(MarkupDiagnosticSeverity.Error, code, message, line, column);
    }

    public static MarkupDiagnostic Warning(string code, string message, int? line = null, int? column = null)
    {
        return new MarkupDiagnostic(MarkupDiagnosticSeverity.Warning, code, message, line, column);
    }
}
