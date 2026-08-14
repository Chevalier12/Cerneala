using Cerneala.LanguageServer.Protocol;
using Cerneala.LanguageServer.Workspace;

namespace Cerneala.LanguageServer.Features;

internal sealed class BuildDiagnosticStore
{
    private readonly object gate = new();
    private Dictionary<string, HashSet<DiagnosticIdentity>> identities = new(PathComparer.Instance);

    public IReadOnlyList<string> Replace(IReadOnlyList<BuildDiagnosticItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        Dictionary<string, HashSet<DiagnosticIdentity>> replacement = new(PathComparer.Instance);
        foreach (BuildDiagnosticItem item in items)
        {
            string path = PathComparer.FromUri(item.Uri);
            if (!replacement.TryGetValue(path, out HashSet<DiagnosticIdentity>? documentIdentities))
            {
                documentIdentities = new HashSet<DiagnosticIdentity>();
                replacement[path] = documentIdentities;
            }

            documentIdentities.Add(DiagnosticIdentity.From(item));
        }

        lock (gate)
        {
            string[] affected = identities.Keys
                .Concat(replacement.Keys)
                .Distinct(PathComparer.Instance)
                .Select(path => new Uri(path).AbsoluteUri)
                .ToArray();
            identities = replacement;
            return affected;
        }
    }

    public IReadOnlyList<LspDiagnostic> RemoveDuplicates(string uri, IReadOnlyList<LspDiagnostic> diagnostics)
    {
        string path = PathComparer.FromUri(uri);
        HashSet<DiagnosticIdentity>? buildDiagnostics;
        lock (gate)
        {
            if (!identities.TryGetValue(path, out HashSet<DiagnosticIdentity>? stored))
            {
                return diagnostics;
            }

            buildDiagnostics = new HashSet<DiagnosticIdentity>(stored);
        }

        return diagnostics
            .Where(diagnostic => !buildDiagnostics.Contains(DiagnosticIdentity.From(diagnostic)))
            .ToArray();
    }

    private readonly record struct DiagnosticIdentity(
        string Code,
        int StartLine,
        int StartCharacter,
        int EndLine,
        int EndCharacter,
        string Message)
    {
        public static DiagnosticIdentity From(BuildDiagnosticItem item) => new(
            item.Code,
            item.Range.Start.Line,
            item.Range.Start.Character,
            item.Range.End.Line,
            item.Range.End.Character,
            item.Message);

        public static DiagnosticIdentity From(LspDiagnostic diagnostic) => new(
            diagnostic.Code,
            diagnostic.Range.Start.Line,
            diagnostic.Range.Start.Character,
            diagnostic.Range.End.Line,
            diagnostic.Range.End.Character,
            diagnostic.Message);
    }
}
