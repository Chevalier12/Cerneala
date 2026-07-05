using System.Text.Json;

namespace RoslynRepoIndexer.Core;

public static class IndexStore
{
    public const string IndexDirectoryName = ".roslyn-index";
    public const string VersionDirectoryName = "v1";

    public static string GetIndexDirectory(string repoRoot) => Path.Combine(repoRoot, IndexDirectoryName);
    public static string GetVersionDirectory(string repoRoot) => Path.Combine(GetIndexDirectory(repoRoot), VersionDirectoryName);
    public static string GetManifestPath(string repoRoot) => Path.Combine(GetVersionDirectory(repoRoot), "manifest.json");
    public static string GetExactReferenceCacheDirectory(string repoRoot) => Path.Combine(GetVersionDirectory(repoRoot), "exact-refs-cache");

    public static bool Exists(string repoRoot) => ResolveReadableVersionDirectory(repoRoot) is not null;

    public static IndexTimingSummary Write(string repoRoot, IndexSnapshot snapshot)
    {
        var indexRoot = GetIndexDirectory(repoRoot);
        var target = GetVersionDirectory(repoRoot);
        Directory.CreateDirectory(indexRoot);
        var temp = Path.Combine(indexRoot, "tmp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);

        var persistStopwatch = System.Diagnostics.Stopwatch.StartNew();
        WriteJsonLines(Path.Combine(temp, "documents.jsonl"), snapshot.Documents);
        WriteJsonLines(Path.Combine(temp, "symbols.jsonl"), snapshot.Symbols);
        WriteJsonLines(Path.Combine(temp, "references.jsonl"), snapshot.References);
        WriteJsonLines(Path.Combine(temp, "tokens.jsonl"), snapshot.Tokens);
        var persistMs = persistStopwatch.ElapsedMilliseconds;
        var timings = snapshot.Manifest.Timings with
        {
            PersistMs = persistMs,
            TotalMs = snapshot.Manifest.Timings.TotalMs + persistMs
        };
        var manifest = snapshot.Manifest with { Timings = timings };
        File.WriteAllText(Path.Combine(temp, "manifest.json"), JsonSerializer.Serialize(manifest, JsonOptions.Default));
        WriteJsonLines(Path.Combine(temp, "diagnostics.jsonl"), BuildDiagnostics(manifest));

        _ = ReadFromDirectory(temp);

        var backup = Path.Combine(indexRoot, "old-" + Guid.NewGuid().ToString("N"));
        try
        {
            if (Directory.Exists(target))
            {
                Directory.Move(target, backup);
            }

            Directory.Move(temp, target);
            if (Directory.Exists(backup))
            {
                Directory.Delete(backup, recursive: true);
            }
        }
        catch
        {
            if (!Directory.Exists(target) && Directory.Exists(backup))
            {
                Directory.Move(backup, target);
            }

            if (Directory.Exists(temp))
            {
                Directory.Delete(temp, recursive: true);
            }

            throw;
        }

        return timings;
    }

    public static IndexSnapshot Read(string repoRoot)
    {
        var directory = ResolveReadableVersionDirectory(repoRoot);
        if (directory is null)
        {
            throw new FileNotFoundException("Index is missing. Run 'ri index' first.");
        }

        IndexSnapshot snapshot;
        try
        {
            snapshot = ReadFromDirectory(directory);
        }
        catch (Exception ex) when (IsIndexReadFailure(ex))
        {
            throw new IndexUnavailableException($"Index is corrupt or incomplete. Run 'ri index --force' to rebuild the index. {ex.Message}");
        }

        if (snapshot.Manifest.SchemaVersion != IndexManifest.CurrentSchemaVersion)
        {
            throw new IndexUnavailableException($"Index schema version {snapshot.Manifest.SchemaVersion} is incompatible with current schema version {IndexManifest.CurrentSchemaVersion}. Run 'ri index --force' to rebuild the index.");
        }

        return snapshot;
    }

    public static StatusSummary GetStatus(string repoRoot)
    {
        var directory = ResolveReadableVersionDirectory(repoRoot);
        if (directory is null)
        {
            return new StatusSummary(IndexStatus.Missing, repoRoot, 0, 0, 0, 0, 0, 0, Array.Empty<string>());
        }

        try
        {
            var snapshot = ReadFromDirectory(directory);
            if (snapshot.Manifest.SchemaVersion != IndexManifest.CurrentSchemaVersion)
            {
                return new StatusSummary(IndexStatus.SchemaIncompatible, snapshot.Manifest.RepoRoot, snapshot.Manifest.SchemaVersion, 0, 0, 0, 0, 0, snapshot.Manifest.RecentWarnings);
            }

            var dirty = snapshot.Manifest.DocumentsByRelativePath.Count(pair =>
            {
                var fullPath = Path.Combine(repoRoot, pair.Key);
                return !File.Exists(fullPath) || new FileInfo(fullPath).Length != pair.Value.Length;
            });

            var status = dirty == 0 ? IndexStatus.Valid : IndexStatus.Stale;
            return new StatusSummary(status, snapshot.Manifest.RepoRoot, snapshot.Manifest.SchemaVersion, snapshot.Documents.Count, snapshot.Symbols.Count, snapshot.References.Count, snapshot.Tokens.Count, dirty, snapshot.Manifest.RecentWarnings);
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidOperationException)
        {
            return new StatusSummary(IndexStatus.Corrupt, repoRoot, 0, 0, 0, 0, 0, 0, new[] { ex.Message });
        }
    }

    public static void Clean(string repoRoot)
    {
        var directory = GetIndexDirectory(repoRoot);
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    public static bool TryReadExactReferenceCache(string repoRoot, string cacheKey, DateTimeOffset indexUpdatedUtc, out IReadOnlyList<SearchResult> results)
    {
        results = Array.Empty<SearchResult>();
        var path = ExactReferenceCachePath(repoRoot, cacheKey);
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            var entry = JsonSerializer.Deserialize<ExactReferenceCacheEntry>(File.ReadAllText(path), JsonOptions.Default);
            if (entry is null || entry.IndexUpdatedUtc != indexUpdatedUtc)
            {
                return false;
            }

            results = entry.Results;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return false;
        }
    }

    public static void WriteExactReferenceCache(string repoRoot, string cacheKey, DateTimeOffset indexUpdatedUtc, IReadOnlyList<SearchResult> results)
    {
        var directory = GetExactReferenceCacheDirectory(repoRoot);
        Directory.CreateDirectory(directory);
        var path = ExactReferenceCachePath(repoRoot, cacheKey);
        var temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
        var entry = new ExactReferenceCacheEntry(indexUpdatedUtc, results.ToArray());
        File.WriteAllText(temp, JsonSerializer.Serialize(entry, JsonOptions.Default));
        File.Move(temp, path, overwrite: true);
    }

    private static IndexSnapshot ReadFromDirectory(string directory)
    {
        var manifest = JsonSerializer.Deserialize<IndexManifest>(File.ReadAllText(Path.Combine(directory, "manifest.json")), JsonOptions.Default)
            ?? throw new InvalidOperationException("Invalid manifest.");
        return new IndexSnapshot(
            manifest,
            ReadJsonLines<DocumentEntry>(Path.Combine(directory, "documents.jsonl")),
            ReadJsonLines<SymbolEntry>(Path.Combine(directory, "symbols.jsonl")),
            ReadJsonLines<ReferenceEntry>(Path.Combine(directory, "references.jsonl")),
            ReadJsonLines<TokenPosting>(Path.Combine(directory, "tokens.jsonl")));
    }

    private static string? ResolveReadableVersionDirectory(string repoRoot)
    {
        var version = GetVersionDirectory(repoRoot);
        if (File.Exists(Path.Combine(version, "manifest.json")))
        {
            return version;
        }

        var indexRoot = GetIndexDirectory(repoRoot);
        if (!Directory.Exists(indexRoot))
        {
            return null;
        }

        return Directory.EnumerateDirectories(indexRoot, "old-*")
            .Where(directory => File.Exists(Path.Combine(directory, "manifest.json")))
            .OrderByDescending(Directory.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static void WriteJsonLines<T>(string path, IReadOnlyList<T> rows)
    {
        using var writer = new StreamWriter(path);
        foreach (var row in rows)
        {
            writer.WriteLine(JsonSerializer.Serialize(row, JsonOptions.Compact));
        }
    }

    private static IReadOnlyList<T> ReadJsonLines<T>(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Required index file is missing: {path}", path);
        }

        var rows = new List<T>();
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                rows.Add(JsonSerializer.Deserialize<T>(line, JsonOptions.Compact) ?? throw new InvalidOperationException($"Invalid row in {path}."));
            }
            catch (JsonException ex)
            {
                throw new JsonException($"Invalid JSON row in {path}.", ex);
            }
        }

        return rows;
    }

    private static bool IsIndexReadFailure(Exception ex)
        => ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException;

    private static IReadOnlyList<IndexDiagnosticEntry> BuildDiagnostics(IndexManifest manifest)
    {
        var rows = new List<IndexDiagnosticEntry>();
        rows.AddRange(manifest.RecentWarnings.Select(warning => new IndexDiagnosticEntry("warning", warning, manifest.UpdatedUtc)));
        rows.Add(TimingDiagnostic("discovery", manifest.Timings.DiscoveryMs, manifest.UpdatedUtc));
        rows.Add(TimingDiagnostic("workspaceLoad", manifest.Timings.WorkspaceLoadMs, manifest.UpdatedUtc));
        rows.Add(TimingDiagnostic("semanticIndex", manifest.Timings.SemanticIndexMs, manifest.UpdatedUtc));
        rows.Add(TimingDiagnostic("textIndex", manifest.Timings.TextIndexMs, manifest.UpdatedUtc));
        rows.Add(TimingDiagnostic("persist", manifest.Timings.PersistMs, manifest.UpdatedUtc));
        rows.Add(TimingDiagnostic("total", manifest.Timings.TotalMs, manifest.UpdatedUtc));
        return rows;
    }

    private static IndexDiagnosticEntry TimingDiagnostic(string stage, long elapsedMs, DateTimeOffset timestampUtc)
        => new("timing", $"{stage} completed in {elapsedMs} ms.", timestampUtc, stage, elapsedMs);

    private static string ExactReferenceCachePath(string repoRoot, string cacheKey)
        => Path.Combine(GetExactReferenceCacheDirectory(repoRoot), ConfigLoader.HashText(cacheKey) + ".json");

    private sealed record IndexDiagnosticEntry(string Severity, string Message, DateTimeOffset TimestampUtc, string? Stage = null, long? ElapsedMs = null);
    private sealed record ExactReferenceCacheEntry(DateTimeOffset IndexUpdatedUtc, IReadOnlyList<SearchResult> Results);
}

public sealed class IndexUnavailableException : Exception
{
    public IndexUnavailableException(string message) : base(message)
    {
    }
}
