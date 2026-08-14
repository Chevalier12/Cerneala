using Cerneala.Language.Diagnostics;
using Cerneala.Language.Semantics;
using Cerneala.Language.Syntax;
using Cerneala.Language.Text;
using Cerneala.LanguageServer.Protocol;
using Cerneala.LanguageServer.Workspace;

namespace Cerneala.LanguageServer.Features;

internal sealed class DiagnosticService(CernealaWorkspace workspace, BuildDiagnosticStore buildDiagnostics)
{
    public Task<VersionedDocumentResult<IReadOnlyList<LspDiagnostic>>?> AnalyzeAsync(
        string uri,
        CancellationToken cancellationToken) =>
        workspace.RunDocumentRequestAsync(
            uri,
            (snapshot, requestCancellation) => Task.FromResult(AnalyzeSnapshot(snapshot, uri, requestCancellation)),
            cancellationToken);

    internal static LspDiagnostic ToLspDiagnostic(SourceText source, LanguageDiagnostic diagnostic)
    {
        int startOffset = Math.Clamp(diagnostic.Span.Start, 0, source.Length);
        int endOffset = Math.Clamp(diagnostic.Span.End, startOffset, source.Length);
        LinePosition start = source.GetLinePosition(startOffset);
        LinePosition end = source.GetLinePosition(endOffset);
        return new LspDiagnostic
        {
            Code = diagnostic.Id,
            Message = diagnostic.Message,
            Severity = ToLspSeverity(diagnostic.Severity),
            Range = new LspRange
            {
                Start = new LspPosition { Line = start.Line, Character = start.Character },
                End = new LspPosition { Line = end.Line, Character = end.Character }
            }
        };
    }

    private IReadOnlyList<LspDiagnostic> AnalyzeSnapshot(
        WorkspaceDocumentSnapshot snapshot,
        string uri,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<LanguageDiagnostic> languageDiagnostics = snapshot.IsStandalone
            ? CreateStandaloneSyntaxDiagnostics(snapshot)
            : snapshot.GetSemanticModels(cancellationToken)
                .SelectMany(model => model.Diagnostics)
                .Where(diagnostic => !DependsOnIncompleteSyntax(snapshot.Syntax, diagnostic))
                .ToList();

        List<LspDiagnostic> diagnostics = languageDiagnostics
            .Select(diagnostic => ToLspDiagnostic(snapshot.Document.Text, diagnostic))
            .Concat(snapshot.InformationDiagnostics.Select(ToLspDiagnostic))
            .GroupBy(DiagnosticKey.From)
            .Select(group => group.First())
            .OrderBy(diagnostic => diagnostic.Range.Start.Line)
            .ThenBy(diagnostic => diagnostic.Range.Start.Character)
            .ThenBy(diagnostic => diagnostic.Range.End.Line)
            .ThenBy(diagnostic => diagnostic.Range.End.Character)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToList();
        return buildDiagnostics.RemoveDuplicates(uri, diagnostics);
    }

    private static List<LanguageDiagnostic> CreateStandaloneSyntaxDiagnostics(WorkspaceDocumentSnapshot snapshot)
    {
        ElementSyntax[] roots = snapshot.Syntax.Children.OfType<ElementSyntax>().ToArray();
        TextSyntax? topLevelText = snapshot.Syntax.Children
            .OfType<TextSyntax>()
            .FirstOrDefault(text =>
                text.Kind is not SyntaxKind.Comment &&
                !string.IsNullOrWhiteSpace(text.Token.Text));
        if (topLevelText is not null || roots.Length != 1)
        {
            return
            [
                CreateMalformedDiagnostic(
                    snapshot,
                    topLevelText?.Span ?? roots.FirstOrDefault()?.Span ?? new TextSpan(0, 0),
                    "Markup must contain exactly one UI root element.")
            ];
        }

        return snapshot.Syntax.Diagnostics
            .Select(diagnostic => CreateMalformedDiagnostic(snapshot, diagnostic.Span, diagnostic.Message))
            .ToList();
    }

    private static LanguageDiagnostic CreateMalformedDiagnostic(
        WorkspaceDocumentSnapshot snapshot,
        TextSpan span,
        string message) => new(
            CernealaDiagnosticCatalog.Get("CERNEALAUI001"),
            span,
            AnalysisMode.Editor,
            Path.GetFileName(snapshot.Document.Path),
            message);

    private static bool DependsOnIncompleteSyntax(DocumentSyntax syntax, LanguageDiagnostic diagnostic)
    {
        if (diagnostic.Id == "CERNEALAUI001")
        {
            return false;
        }

        return syntax.DescendantElements()
            .Where(element => element.HasMissingTokens)
            .Any(element => Contains(element.Span, diagnostic.Span));
    }

    private static bool Contains(TextSpan container, TextSpan candidate) =>
        candidate.Start >= container.Start && candidate.End <= container.End;

    private static LspDiagnostic ToLspDiagnostic(WorkspaceInfoDiagnostic diagnostic) => new()
    {
        Code = diagnostic.Id,
        Message = diagnostic.Message,
        Severity = 3,
        Range = new LspRange
        {
            Start = new LspPosition { Line = 0, Character = 0 },
            End = new LspPosition { Line = 0, Character = 0 }
        }
    };

    private static int ToLspSeverity(LanguageDiagnosticSeverity severity) => severity switch
    {
        LanguageDiagnosticSeverity.Error => 1,
        LanguageDiagnosticSeverity.Warning => 2,
        LanguageDiagnosticSeverity.Information => 3,
        _ => 4
    };

    private readonly record struct DiagnosticKey(
        string Code,
        int StartLine,
        int StartCharacter,
        int EndLine,
        int EndCharacter,
        string Message)
    {
        public static DiagnosticKey From(LspDiagnostic diagnostic) => new(
            diagnostic.Code,
            diagnostic.Range.Start.Line,
            diagnostic.Range.Start.Character,
            diagnostic.Range.End.Line,
            diagnostic.Range.End.Character,
            diagnostic.Message);
    }
}
