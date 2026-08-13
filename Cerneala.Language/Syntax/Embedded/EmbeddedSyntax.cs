using Cerneala.Language.Text;
using Cerneala.Language.Diagnostics;

namespace Cerneala.Language.Syntax.Embedded;

internal enum EmbeddedLanguageKind
{
    Directive,
    Motion,
    Prism
}

internal sealed class EmbeddedDiagnostic
{
    public EmbeddedDiagnostic(string id, string message, TextSpan span, bool transient = false)
    {
        Id = id;
        Message = message;
        Span = span;
        IsTransient = transient;
    }

    public string Id { get; }

    public string Message { get; }

    public TextSpan Span { get; }

    public bool IsTransient { get; }

    public LanguageDiagnosticSeverity GetSeverity(AnalysisMode mode) =>
        IsTransient && mode == AnalysisMode.Editor
            ? LanguageDiagnosticSeverity.Information
            : LanguageDiagnosticSeverity.Error;
}

internal sealed class EmbeddedParseResult<TSyntax>
{
    public EmbeddedParseResult(TSyntax syntax, IReadOnlyList<EmbeddedDiagnostic> diagnostics)
    {
        Syntax = syntax;
        Diagnostics = diagnostics;
    }

    public TSyntax Syntax { get; }

    public IReadOnlyList<EmbeddedDiagnostic> Diagnostics { get; }
}

internal sealed class DirectiveDocumentSyntax
{
    public DirectiveDocumentSyntax(
        string text,
        int absoluteOffset,
        EmbeddedLanguageKind language,
        IReadOnlyList<DirectiveSyntax> directives,
        IReadOnlyList<AssignmentSyntax> assignments)
    {
        Text = text;
        AbsoluteOffset = absoluteOffset;
        Language = language;
        Directives = directives;
        Assignments = assignments;
    }

    public string Text { get; }

    public int AbsoluteOffset { get; }

    public EmbeddedLanguageKind Language { get; }

    public IReadOnlyList<DirectiveSyntax> Directives { get; }

    public IReadOnlyList<AssignmentSyntax> Assignments { get; }
}

internal sealed class DirectiveSyntax
{
    public DirectiveSyntax(string keyword, TextSpan span, int depth)
    {
        Keyword = keyword;
        Span = span;
        Depth = depth;
    }

    public string Keyword { get; }

    public TextSpan Span { get; }

    public int Depth { get; }
}

internal sealed class AssignmentSyntax
{
    public AssignmentSyntax(string name, TextSpan nameSpan, TextSpan valueSpan)
    {
        Name = name;
        NameSpan = nameSpan;
        ValueSpan = valueSpan;
    }

    public string Name { get; }

    public TextSpan NameSpan { get; }

    public TextSpan ValueSpan { get; }
}
