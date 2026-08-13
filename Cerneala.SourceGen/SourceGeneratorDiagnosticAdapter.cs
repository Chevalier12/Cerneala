using Cerneala.Language.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Cerneala.SourceGen;

internal static class SourceGeneratorDiagnosticAdapter
{
    private static readonly IReadOnlyDictionary<string, DiagnosticDescriptor> descriptors =
        CernealaDiagnosticCatalog.All.ToDictionary(
            descriptor => descriptor.Id,
            descriptor => new DiagnosticDescriptor(
                descriptor.Id,
                descriptor.Title,
                descriptor.MessageFormat,
                descriptor.Category,
                ConvertSeverity(descriptor.BuildSeverity),
                isEnabledByDefault: true),
            StringComparer.Ordinal);

    public static DiagnosticDescriptor GetDescriptor(string id) => descriptors[id];

    public static Diagnostic ToDiagnostic(
        LanguageDiagnostic diagnostic,
        string path,
        Microsoft.CodeAnalysis.Text.SourceText source)
    {
        int start = Math.Max(0, Math.Min(source.Length, diagnostic.Span.Start));
        int end = Math.Max(start, Math.Min(source.Length, diagnostic.Span.End));
        Microsoft.CodeAnalysis.Text.TextSpan span = Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(start, end);
        Location location = Location.Create(path, span, source.Lines.GetLinePositionSpan(span));
        return Diagnostic.Create(
            GetDescriptor(diagnostic.Id),
            location,
            diagnostic.Arguments.ToArray());
    }

    private static DiagnosticSeverity ConvertSeverity(LanguageDiagnosticSeverity severity) => severity switch
    {
        LanguageDiagnosticSeverity.Information => DiagnosticSeverity.Info,
        LanguageDiagnosticSeverity.Warning => DiagnosticSeverity.Warning,
        LanguageDiagnosticSeverity.Error => DiagnosticSeverity.Error,
        _ => DiagnosticSeverity.Hidden
    };
}
