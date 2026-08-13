namespace Cerneala.Language.Diagnostics;

internal sealed class LanguageDiagnosticDescriptor
{
    public LanguageDiagnosticDescriptor(
        string id,
        string title,
        string messageFormat,
        string category,
        LanguageDiagnosticSeverity buildSeverity,
        LanguageDiagnosticSeverity editorSeverity)
    {
        Id = id;
        Title = title;
        MessageFormat = messageFormat;
        Category = category;
        BuildSeverity = buildSeverity;
        EditorSeverity = editorSeverity;
    }

    public string Id { get; }

    public string Title { get; }

    public string MessageFormat { get; }

    public string Category { get; }

    public LanguageDiagnosticSeverity BuildSeverity { get; }

    public LanguageDiagnosticSeverity EditorSeverity { get; }

    public LanguageDiagnosticSeverity GetSeverity(AnalysisMode mode) =>
        mode == AnalysisMode.Build ? BuildSeverity : EditorSeverity;
}
