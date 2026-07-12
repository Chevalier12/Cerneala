using System.Diagnostics;

namespace RoslynRepoIndexer.Core;

public enum ChangeComparison { PreviousGeneration, WorkingTreeHead, Generations, Commits }
public enum SemanticChangeKind { Added, Removed, Modified, Touched }

public sealed record ChangedFile(string Path, string Status);
public sealed record ChangedSymbol(SemanticChangeKind Kind, string SymbolId, SymbolSummary? Before, SymbolSummary? After, bool SignatureChanged, bool PublicApiChange);
public sealed record SemanticChangesResult(
    string Comparison,
    string? BaseId,
    string? TargetId,
    IReadOnlyList<ChangedFile> Files,
    IReadOnlyList<ChangedSymbol> Symbols,
    IReadOnlyList<string> AffectedProjects,
    bool Truncated);

public sealed class SemanticChangeService
{
    private readonly string repoRoot;

    public SemanticChangeService(string repoRoot)
        => this.repoRoot = Path.GetFullPath(repoRoot);

    public SemanticChangesResult Compare(ChangeComparison comparison, string? baseId = null, string? targetId = null, int maxResults = 500)
        => comparison switch
        {
            ChangeComparison.PreviousGeneration => CompareSnapshots(IndexStore.ReadPrevious(repoRoot), IndexStore.Read(repoRoot), "previous-generation", maxResults),
            ChangeComparison.Generations => CompareSnapshots(
                string.IsNullOrWhiteSpace(baseId) ? IndexStore.ReadPrevious(repoRoot) : IndexStore.ReadGeneration(repoRoot, baseId),
                string.IsNullOrWhiteSpace(targetId) ? IndexStore.Read(repoRoot) : IndexStore.ReadGeneration(repoRoot, targetId),
                "generations",
                maxResults),
            ChangeComparison.WorkingTreeHead => CompareGit("HEAD", null, "working-tree-head", maxResults),
            ChangeComparison.Commits when !string.IsNullOrWhiteSpace(baseId) && !string.IsNullOrWhiteSpace(targetId) => CompareGit(baseId, targetId, "commits", maxResults),
            ChangeComparison.Commits => throw new ArgumentException("Commit comparison requires both baseId and targetId."),
            _ => throw new ArgumentOutOfRangeException(nameof(comparison))
        };

    private SemanticChangesResult CompareSnapshots(IndexSnapshot before, IndexSnapshot after, string comparison, int maxResults)
    {
        var beforeSymbols = before.Symbols.GroupBy(symbol => symbol.SymbolId, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var afterSymbols = after.Symbols.GroupBy(symbol => symbol.SymbolId, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var changes = new List<ChangedSymbol>();
        foreach (var id in beforeSymbols.Keys.Union(afterSymbols.Keys, StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal))
        {
            beforeSymbols.TryGetValue(id, out var oldSymbol);
            afterSymbols.TryGetValue(id, out var newSymbol);
            if (oldSymbol is null)
            {
                changes.Add(Change(SemanticChangeKind.Added, null, newSymbol!));
            }
            else if (newSymbol is null)
            {
                changes.Add(Change(SemanticChangeKind.Removed, oldSymbol, null));
            }
            else if (!Equivalent(oldSymbol, newSymbol))
            {
                changes.Add(Change(SemanticChangeKind.Modified, oldSymbol, newSymbol));
            }
        }

        var beforeDocuments = before.Documents.GroupBy(document => document.RelativePath, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var afterDocuments = after.Documents.GroupBy(document => document.RelativePath, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var files = beforeDocuments.Keys.Union(afterDocuments.Keys, StringComparer.Ordinal).OrderBy(path => path, StringComparer.Ordinal).Select(path =>
        {
            beforeDocuments.TryGetValue(path, out var oldDocument); afterDocuments.TryGetValue(path, out var newDocument);
            var status = oldDocument is null ? "added" : newDocument is null ? "removed" : oldDocument.ContentHash == newDocument.ContentHash ? null : "modified";
            return status is null ? null : new ChangedFile(path, status);
        }).Where(change => change is not null).Cast<ChangedFile>().ToArray();
        var affectedProjects = changes.Select(change => change.After?.ProjectName ?? change.Before?.ProjectName).Where(project => project is not null).Cast<string>().Distinct(StringComparer.Ordinal).OrderBy(project => project, StringComparer.Ordinal).ToArray();
        var limit = Math.Clamp(maxResults, 1, 10_000);
        return new SemanticChangesResult(comparison, before.Manifest.GenerationId, after.Manifest.GenerationId, files.Take(limit).ToArray(), changes.Take(limit).ToArray(), affectedProjects, files.Length > limit || changes.Count > limit);
    }

    private SemanticChangesResult CompareGit(string baseRevision, string? targetRevision, string comparison, int maxResults)
    {
        var arguments = new List<string> { "diff", "--name-status", "--no-renames", baseRevision };
        if (targetRevision is not null) arguments.Add(targetRevision);
        arguments.Add("--");
        var files = ParseGitChanges(RunGit(arguments));
        if (targetRevision is null)
        {
            var untracked = RunGit(new[] { "ls-files", "--others", "--exclude-standard" })
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(path => new ChangedFile(RepositoryDiscovery.NormalizeRelative(path), "added"));
            files = files.Concat(untracked).GroupBy(file => file.Path, StringComparer.Ordinal).Select(group => group.Last()).OrderBy(file => file.Path, StringComparer.Ordinal).ToArray();
        }

        var current = IndexStore.Read(repoRoot);
        var changedPaths = files.Select(file => file.Path).ToHashSet(StringComparer.Ordinal);
        var symbols = current.Symbols.Where(symbol => changedPaths.Contains(symbol.Path)).OrderBy(symbol => symbol.Path, StringComparer.Ordinal).ThenBy(symbol => symbol.SpanStart).Select(symbol => new ChangedSymbol(SemanticChangeKind.Touched, symbol.SymbolId, null, Summary(symbol), false, IsPublic(symbol))).ToArray();
        var projects = symbols.Select(symbol => symbol.After?.ProjectName).Where(project => project is not null).Cast<string>().Distinct(StringComparer.Ordinal).OrderBy(project => project, StringComparer.Ordinal).ToArray();
        var limit = Math.Clamp(maxResults, 1, 10_000);
        return new SemanticChangesResult(comparison, baseRevision, targetRevision ?? "working-tree", files.Take(limit).ToArray(), symbols.Take(limit).ToArray(), projects, files.Length > limit || symbols.Length > limit);
    }

    private static ChangedSymbol Change(SemanticChangeKind kind, SymbolEntry? before, SymbolEntry? after)
    {
        var signatureChanged = before is not null && after is not null && !string.Equals(before.Signature, after.Signature, StringComparison.Ordinal);
        return new ChangedSymbol(kind, after?.SymbolId ?? before!.SymbolId, before is null ? null : Summary(before), after is null ? null : Summary(after), signatureChanged, IsPublic(before) || IsPublic(after));
    }

    private static bool Equivalent(SymbolEntry left, SymbolEntry right)
        => left.Signature == right.Signature
           && left.Accessibility == right.Accessibility
           && left.Path == right.Path
           && left.ContainerName == right.ContainerName
           && left.ReturnType == right.ReturnType
           && left.Modifiers.SequenceEqual(right.Modifiers, StringComparer.Ordinal)
           && left.ParameterTypes.SequenceEqual(right.ParameterTypes, StringComparer.Ordinal)
           && left.BaseTypeIds.SequenceEqual(right.BaseTypeIds, StringComparer.Ordinal)
           && left.InterfaceTypeIds.SequenceEqual(right.InterfaceTypeIds, StringComparer.Ordinal)
           && left.OverriddenSymbolId == right.OverriddenSymbolId;

    private static SymbolSummary Summary(SymbolEntry symbol)
        => new(symbol.SymbolId, symbol.Name, symbol.Kind, symbol.FullyQualifiedName, symbol.Signature, symbol.Accessibility, new SourceSpanSummary(symbol.Path, symbol.Line, symbol.Column, symbol.EndLine, symbol.EndColumn, symbol.SpanStart, symbol.SpanLength), symbol.ProjectName, symbol.ContainerName);

    private static bool IsPublic(SymbolEntry? symbol)
        => symbol?.Accessibility is "public" or "protected" or "protected internal";

    private static ChangedFile[] ParseGitChanges(string output)
        => output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(line =>
        {
            var separator = line.IndexOf('\t');
            if (separator < 0) throw new InvalidDataException($"Invalid git diff record '{line}'.");
            var status = line[..separator] switch { "A" => "added", "D" => "removed", "M" => "modified", var value => value.ToLowerInvariant() };
            return new ChangedFile(RepositoryDiscovery.NormalizeRelative(line[(separator + 1)..]), status);
        }).OrderBy(file => file.Path, StringComparer.Ordinal).ToArray();

    private string RunGit(IEnumerable<string> arguments)
    {
        using var process = Process.Start(new ProcessStartInfo("git") { WorkingDirectory = repoRoot, RedirectStandardOutput = true, RedirectStandardError = true }.WithArguments(arguments.ToArray()))
            ?? throw new InvalidOperationException("Failed to start git.");
        var output = process.StandardOutput.ReadToEnd(); var error = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(5_000) || process.ExitCode != 0) throw new InvalidOperationException($"Git comparison failed: {error.Trim()}");
        return output;
    }
}
