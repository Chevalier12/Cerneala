using System.Text.Json;

namespace RoslynRepoIndexer.Core;

public static class IndexStore
{
    public const string IndexDirectoryName = ".roslyn-index";
    public const string VersionDirectoryName = "v1";
    public const string GenerationsDirectoryName = "generations";
    public const string CurrentPointerFileName = "current.json";
    private const string StateFileName = "state.json";

    public static string GetIndexDirectory(string repoRoot) => Path.Combine(repoRoot, IndexDirectoryName);
    public static string GetVersionDirectory(string repoRoot) => ResolveReadableVersionDirectory(repoRoot) ?? Path.Combine(GetIndexDirectory(repoRoot), VersionDirectoryName);
    public static string GetGenerationsDirectory(string repoRoot) => Path.Combine(GetIndexDirectory(repoRoot), GenerationsDirectoryName);
    public static string GetGenerationDirectory(string repoRoot, string generationId) => Path.Combine(GetGenerationsDirectory(repoRoot), generationId);
    public static string GetCurrentPointerPath(string repoRoot) => Path.Combine(GetIndexDirectory(repoRoot), CurrentPointerFileName);
    public static string GetManifestPath(string repoRoot) => Path.Combine(GetVersionDirectory(repoRoot), "manifest.json");
    public static string GetExactReferenceCacheDirectory(string repoRoot) => Path.Combine(GetIndexDirectory(repoRoot), "exact-refs-cache");

    public static bool Exists(string repoRoot) => ResolveReadableVersionDirectory(repoRoot) is not null;

    public static IndexGenerationStamp GetGenerationStamp(string repoRoot)
    {
        var directory = ResolveReadableVersionDirectory(repoRoot)
            ?? throw new FileNotFoundException("Index is missing. Run 'ri index' first.");
        var path = Path.Combine(directory, "manifest.json");
        var file = new FileInfo(path);
        using var stream = File.OpenRead(path);
        var manifest = JsonSerializer.Deserialize<IndexManifest>(stream, JsonOptions.Default)
            ?? throw new IndexUnavailableException("Index manifest is invalid. Run 'ri index --force' to rebuild the index.");
        return new IndexGenerationStamp(manifest.GenerationId, manifest.UpdatedUtc, file.Length);
    }

    public static IndexTimingSummary Write(string repoRoot, IndexSnapshot snapshot)
        => WriteCore(repoRoot, snapshot, dirtyDocumentIds: null);

    internal static IndexTimingSummary WriteIncremental(string repoRoot, IndexSnapshot snapshot, IReadOnlySet<string> dirtyDocumentIds)
        => WriteCore(repoRoot, snapshot, dirtyDocumentIds);

    private static IndexTimingSummary WriteCore(string repoRoot, IndexSnapshot snapshot, IReadOnlySet<string>? dirtyDocumentIds)
    {
        var indexRoot = GetIndexDirectory(repoRoot);
        Directory.CreateDirectory(indexRoot);
        using var writeLock = IndexWriteLock.Acquire(indexRoot);
        var previousGenerationDirectory = ResolveReadableVersionDirectory(repoRoot);
        var generationsRoot = GetGenerationsDirectory(repoRoot);
        Directory.CreateDirectory(generationsRoot);
        var generationId = string.IsNullOrWhiteSpace(snapshot.Manifest.GenerationId) ? Guid.NewGuid().ToString("N") : snapshot.Manifest.GenerationId;
        var target = GetGenerationDirectory(repoRoot, generationId);
        var temp = Path.Combine(indexRoot, "tmp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);

        var persistStopwatch = System.Diagnostics.Stopwatch.StartNew();
        SegmentPersistenceSummary? segmentSummary = null;
        if (string.Equals(snapshot.Manifest.StorageFormat, "segmented-binary-v1", StringComparison.Ordinal))
        {
            segmentSummary = SegmentedIndexCodec.Write(indexRoot, temp, snapshot, previousGenerationDirectory, dirtyDocumentIds);
        }
        else
        {
            BinaryIndexCodec.Write(temp, snapshot);
        }
        var persistMs = persistStopwatch.ElapsedMilliseconds;
        var timings = snapshot.Manifest.Timings with
        {
            PersistMs = persistMs,
            TotalMs = snapshot.Manifest.Timings.TotalMs + persistMs
        };
        var manifest = snapshot.Manifest with
        {
            GenerationId = generationId,
            Timings = timings,
            SegmentCount = segmentSummary?.SegmentCount ?? 0,
            SegmentsWritten = segmentSummary?.SegmentsWritten ?? 0,
            SegmentsReused = segmentSummary?.SegmentsReused ?? 0,
            SegmentBytes = segmentSummary?.SegmentBytes ?? 0
        };
        File.WriteAllText(Path.Combine(temp, "manifest.json"), JsonSerializer.Serialize(manifest, JsonOptions.Default));
        File.WriteAllText(Path.Combine(temp, StateFileName), JsonSerializer.Serialize(IndexFastState.FromManifest(manifest), JsonOptions.Compact));
        WriteJsonLines(Path.Combine(temp, "diagnostics.jsonl"), BuildDiagnostics(manifest));

        _ = ReadFromDirectory(temp);

        var previousPointer = TryReadPointer(repoRoot);
        var previousGenerationId = previousPointer is not null && IsGenerationValid(repoRoot, previousPointer.CurrentGenerationId)
            ? previousPointer.CurrentGenerationId
            : null;
        try
        {
            if (Directory.Exists(target))
            {
                Directory.Delete(target, recursive: true);
            }

            Directory.Move(temp, target);
            var pointer = new GenerationPointer(generationId, previousGenerationId, DateTimeOffset.UtcNow);
            WritePointerAtomic(repoRoot, pointer);
            CleanupOldGenerations(repoRoot, pointer);
        }
        catch
        {
            if (Directory.Exists(temp))
            {
                Directory.Delete(temp, recursive: true);
            }

            throw;
        }

        return timings;
    }

    public static IndexSnapshot ReadPrevious(string repoRoot)
    {
        var pointer = TryReadPointer(repoRoot);
        if (pointer?.PreviousGenerationId is null)
        {
            throw new FileNotFoundException("Previous index generation is not available.");
        }

        return ReadGeneration(repoRoot, pointer.PreviousGenerationId);
    }

    public static IndexSnapshot ReadGeneration(string repoRoot, string generationId)
    {
        var directory = GetGenerationDirectory(repoRoot, generationId);
        if (!Directory.Exists(directory))
        {
            throw new FileNotFoundException($"Index generation '{generationId}' is missing.");
        }

        try
        {
            var snapshot = ReadFromDirectory(directory);
            if (!string.Equals(snapshot.Manifest.GenerationId, generationId, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Generation directory and manifest IDs do not match.");
            }
            return snapshot;
        }
        catch (Exception ex) when (IsIndexReadFailure(ex))
        {
            throw new IndexUnavailableException($"Index generation '{generationId}' is corrupt. {ex.Message}");
        }
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
            var previous = TryResolvePreviousGeneration(repoRoot, directory);
            if (previous is not null)
            {
                try
                {
                    return ReadFromDirectory(previous);
                }
                catch (Exception fallbackException) when (IsIndexReadFailure(fallbackException))
                {
                    throw new IndexUnavailableException($"Current and previous index generations are corrupt or incomplete. Run 'ri index --force' to rebuild the index. Current: {ex.Message} Previous: {fallbackException.Message}");
                }
            }
            throw new IndexUnavailableException($"Index is corrupt or incomplete. Run 'ri index --force' to rebuild the index. {ex.Message}");
        }

        if (snapshot.Manifest.SchemaVersion != IndexManifest.CurrentSchemaVersion)
        {
            throw new IndexUnavailableException($"Index schema version {snapshot.Manifest.SchemaVersion} is incompatible with current schema version {IndexManifest.CurrentSchemaVersion}. Run 'ri index --force' to rebuild the index.");
        }

        return snapshot;
    }

    public static IndexManifest ReadManifest(string repoRoot)
    {
        var directory = ResolveReadableVersionDirectory(repoRoot)
            ?? throw new FileNotFoundException("Index is missing. Run 'ri index' first.");
        try
        {
            return JsonSerializer.Deserialize<IndexManifest>(
                       File.ReadAllText(Path.Combine(directory, "manifest.json")),
                       JsonOptions.Default)
                   ?? throw new InvalidOperationException("Invalid manifest.");
        }
        catch (Exception ex) when (IsIndexReadFailure(ex))
        {
            throw new IndexUnavailableException($"Index manifest is corrupt or incomplete. Run 'ri index --force' to rebuild the index. {ex.Message}");
        }
    }

    internal static IndexManifest ReadFastManifest(string repoRoot)
    {
        var directory = ResolveReadableVersionDirectory(repoRoot)
            ?? throw new FileNotFoundException("Index is missing. Run 'ri index' first.");
        var statePath = Path.Combine(directory, StateFileName);
        if (!File.Exists(statePath))
        {
            var manifest = ReadManifest(repoRoot);
            ValidateGenerationFiles(repoRoot, manifest);
            return manifest;
        }

        var state = JsonSerializer.Deserialize<IndexFastState>(File.ReadAllText(statePath), JsonOptions.Compact)
            ?? throw new InvalidDataException("Index state is invalid.");
        if (!string.Equals(Path.GetFileName(directory), VersionDirectoryName, StringComparison.Ordinal) &&
            !string.Equals(Path.GetFileName(directory), state.GenerationId, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Index state generation does not match its directory.");
        }
        if (string.Equals(state.StorageFormat, "segmented-binary-v1", StringComparison.Ordinal) &&
            !File.Exists(Path.Combine(directory, "segments.json")))
        {
            throw new InvalidDataException("Segment descriptor is missing.");
        }

        return state.ToManifest();
    }

    public static void ValidateGenerationFiles(string repoRoot, IndexManifest manifest)
    {
        var directory = ResolveReadableVersionDirectory(repoRoot)
            ?? throw new FileNotFoundException("Index is missing. Run 'ri index' first.");
        if (string.Equals(manifest.StorageFormat, "binary-v1", StringComparison.Ordinal))
        {
            BinaryIndexCodec.ValidateHeaders(directory, manifest);
        }
        else if (string.Equals(manifest.StorageFormat, "segmented-binary-v1", StringComparison.Ordinal))
        {
            SegmentedIndexCodec.ValidateDescriptorMetadata(directory, manifest);
        }
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
            var manifest = ReadManifestFromDirectory(directory);
            if (manifest.SchemaVersion != IndexManifest.CurrentSchemaVersion)
            {
                return new StatusSummary(IndexStatus.SchemaIncompatible, manifest.RepoRoot, manifest.SchemaVersion, 0, 0, 0, 0, 0, manifest.RecentWarnings);
            }

            if (string.Equals(manifest.StorageFormat, "binary-v1", StringComparison.Ordinal))
            {
                BinaryIndexCodec.ValidateHeaders(directory, manifest);
            }
            else if (string.Equals(manifest.StorageFormat, "segmented-binary-v1", StringComparison.Ordinal))
            {
                SegmentedIndexCodec.ValidateDescriptor(GetIndexDirectory(repoRoot), directory, manifest);
            }

            var dirty = manifest.DocumentsByRelativePath.Count(pair =>
            {
                var fullPath = Path.Combine(repoRoot, pair.Key);
                return !File.Exists(fullPath) || new FileInfo(fullPath).Length != pair.Value.Length;
            });

            var status = dirty == 0 ? IndexStatus.Valid : IndexStatus.Stale;
            return new StatusSummary(status, manifest.RepoRoot, manifest.SchemaVersion, manifest.DocumentCount, manifest.SymbolCount, manifest.ReferenceCount, manifest.TokenCount, dirty, manifest.RecentWarnings);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or InvalidOperationException)
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
        var manifest = ReadManifestFromDirectory(directory);
        if (string.Equals(manifest.StorageFormat, "binary-v1", StringComparison.Ordinal))
        {
            return BinaryIndexCodec.Read(directory, manifest);
        }
        if (string.Equals(manifest.StorageFormat, "segmented-binary-v1", StringComparison.Ordinal))
        {
            var parent = Directory.GetParent(directory) ?? throw new InvalidDataException("Cannot resolve index root for segmented generation.");
            var indexRoot = string.Equals(parent.Name, GenerationsDirectoryName, StringComparison.Ordinal)
                ? parent.Parent?.FullName ?? throw new InvalidDataException("Cannot resolve index root for segmented generation.")
                : parent.FullName;
            return SegmentedIndexCodec.Read(indexRoot, directory, manifest);
        }

        return new IndexSnapshot(manifest, ReadJsonLines<DocumentEntry>(Path.Combine(directory, "documents.jsonl")), ReadJsonLines<SymbolEntry>(Path.Combine(directory, "symbols.jsonl")), ReadJsonLines<ReferenceEntry>(Path.Combine(directory, "references.jsonl")), ReadJsonLines<TokenPosting>(Path.Combine(directory, "tokens.jsonl")));
    }

    private static IndexManifest ReadManifestFromDirectory(string directory)
        => JsonSerializer.Deserialize<IndexManifest>(File.ReadAllText(Path.Combine(directory, "manifest.json")), JsonOptions.Default)
           ?? throw new InvalidOperationException("Invalid manifest.");

    private static string? ResolveReadableVersionDirectory(string repoRoot)
    {
        var pointer = TryReadPointer(repoRoot);
        if (pointer is not null)
        {
            var current = GetGenerationDirectory(repoRoot, pointer.CurrentGenerationId);
            if (File.Exists(Path.Combine(current, "manifest.json")))
            {
                return current;
            }

            if (pointer.PreviousGenerationId is not null)
            {
                var previous = GetGenerationDirectory(repoRoot, pointer.PreviousGenerationId);
                if (File.Exists(Path.Combine(previous, "manifest.json")))
                {
                    return previous;
                }
            }
        }

        var legacy = Path.Combine(GetIndexDirectory(repoRoot), VersionDirectoryName);
        if (File.Exists(Path.Combine(legacy, "manifest.json")))
        {
            return legacy;
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

    private static GenerationPointer? TryReadPointer(string repoRoot)
    {
        var path = GetCurrentPointerPath(repoRoot);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<GenerationPointer>(File.ReadAllText(path), JsonOptions.Compact);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return null;
        }
    }

    private static string? TryResolvePreviousGeneration(string repoRoot, string failedDirectory)
    {
        var pointer = TryReadPointer(repoRoot);
        if (pointer?.PreviousGenerationId is null) return null;
        var current = GetGenerationDirectory(repoRoot, pointer.CurrentGenerationId);
        if (!string.Equals(Path.GetFullPath(current), Path.GetFullPath(failedDirectory), StringComparison.OrdinalIgnoreCase)) return null;
        var previous = GetGenerationDirectory(repoRoot, pointer.PreviousGenerationId);
        return File.Exists(Path.Combine(previous, "manifest.json")) ? previous : null;
    }

    private static void WritePointerAtomic(string repoRoot, GenerationPointer pointer)
    {
        var path = GetCurrentPointerPath(repoRoot);
        var temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
        File.WriteAllText(temp, JsonSerializer.Serialize(pointer, JsonOptions.Compact));
        File.Move(temp, path, overwrite: true);
    }

    private static void CleanupOldGenerations(string repoRoot, GenerationPointer pointer)
    {
        var generationsRoot = GetGenerationsDirectory(repoRoot);
        var keep = new HashSet<string>(StringComparer.Ordinal) { pointer.CurrentGenerationId };
        if (pointer.PreviousGenerationId is not null)
        {
            keep.Add(pointer.PreviousGenerationId);
        }

        var resolvedRoot = Path.GetFullPath(generationsRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        foreach (var directory in Directory.EnumerateDirectories(generationsRoot))
        {
            var fullPath = Path.GetFullPath(directory);
            if (!fullPath.StartsWith(resolvedRoot, StringComparison.OrdinalIgnoreCase) || keep.Contains(Path.GetFileName(directory)))
            {
                continue;
            }

            Directory.Delete(fullPath, recursive: true);
        }

        var legacy = Path.GetFullPath(Path.Combine(GetIndexDirectory(repoRoot), VersionDirectoryName));
        var indexRoot = Path.GetFullPath(GetIndexDirectory(repoRoot)).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (legacy.StartsWith(indexRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(legacy))
        {
            Directory.Delete(legacy, recursive: true);
        }

        CleanupUnreferencedSegments(repoRoot, keep);
    }

    private static void CleanupUnreferencedSegments(string repoRoot, IReadOnlySet<string> retainedGenerations)
    {
        var referenced = new HashSet<string>(StringComparer.Ordinal);
        foreach (var generationId in retainedGenerations)
        {
            var directory = GetGenerationDirectory(repoRoot, generationId);
            var manifestPath = Path.Combine(directory, "manifest.json");
            if (!File.Exists(manifestPath)) continue;
            try
            {
                var manifest = ReadManifestFromDirectory(directory);
                if (string.Equals(manifest.StorageFormat, "segmented-binary-v1", StringComparison.Ordinal))
                {
                    referenced.UnionWith(SegmentedIndexCodec.ReadReferencedSegmentFiles(directory));
                }
            }
            catch (Exception ex) when (IsIndexReadFailure(ex))
            {
                // A broken retired generation must not make publishing the new generation fail.
            }
        }

        var pool = Path.Combine(GetIndexDirectory(repoRoot), "segments");
        if (!Directory.Exists(pool)) return;
        var resolvedPool = Path.GetFullPath(pool).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        foreach (var path in Directory.EnumerateFiles(pool, "*.bin"))
        {
            var fullPath = Path.GetFullPath(path);
            if (fullPath.StartsWith(resolvedPool, StringComparison.OrdinalIgnoreCase) && !referenced.Contains(Path.GetFileName(path)))
            {
                File.Delete(fullPath);
            }
        }
    }

    private static bool IsGenerationValid(string repoRoot, string generationId)
    {
        try
        {
            var directory = GetGenerationDirectory(repoRoot, generationId);
            var manifest = ReadManifestFromDirectory(directory);
            if (manifest.SchemaVersion != IndexManifest.CurrentSchemaVersion ||
                !string.Equals(manifest.GenerationId, generationId, StringComparison.Ordinal))
            {
                return false;
            }

            if (string.Equals(manifest.StorageFormat, "segmented-binary-v1", StringComparison.Ordinal))
            {
                SegmentedIndexCodec.ValidateDescriptor(GetIndexDirectory(repoRoot), directory, manifest);
            }
            else if (string.Equals(manifest.StorageFormat, "binary-v1", StringComparison.Ordinal))
            {
                BinaryIndexCodec.ValidateHeaders(directory, manifest);
            }

            return true;
        }
        catch (Exception ex) when (IsIndexReadFailure(ex))
        {
            return false;
        }
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
        => ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or InvalidOperationException;

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
    private sealed record GenerationPointer(string CurrentGenerationId, string? PreviousGenerationId, DateTimeOffset PublishedUtc);
    private sealed record IndexFastState(
        int SchemaVersion,
        string GenerationId,
        string StorageFormat,
        string RepoRoot,
        DateTimeOffset CreatedUtc,
        DateTimeOffset UpdatedUtc,
        string ConfigHash,
        string WorkspaceInputsHash,
        string DiscoveryFingerprint,
        string RepositoryStateFingerprint,
        int DocumentCount,
        int SymbolCount,
        int ReferenceCount,
        int TokenCount,
        int WarningCount,
        int SegmentCount,
        IReadOnlyList<string> RecentWarnings)
    {
        public static IndexFastState FromManifest(IndexManifest manifest)
            => new(manifest.SchemaVersion, manifest.GenerationId, manifest.StorageFormat, manifest.RepoRoot, manifest.CreatedUtc, manifest.UpdatedUtc, manifest.ConfigHash, manifest.WorkspaceInputsHash, manifest.DiscoveryFingerprint, manifest.RepositoryStateFingerprint, manifest.DocumentCount, manifest.SymbolCount, manifest.ReferenceCount, manifest.TokenCount, manifest.WarningCount, manifest.SegmentCount, manifest.RecentWarnings);

        public IndexManifest ToManifest()
            => new()
            {
                SchemaVersion = SchemaVersion,
                GenerationId = GenerationId,
                StorageFormat = StorageFormat,
                RepoRoot = RepoRoot,
                CreatedUtc = CreatedUtc,
                UpdatedUtc = UpdatedUtc,
                ConfigHash = ConfigHash,
                WorkspaceInputsHash = WorkspaceInputsHash,
                DiscoveryFingerprint = DiscoveryFingerprint,
                RepositoryStateFingerprint = RepositoryStateFingerprint,
                DocumentCount = DocumentCount,
                SymbolCount = SymbolCount,
                ReferenceCount = ReferenceCount,
                TokenCount = TokenCount,
                WarningCount = WarningCount,
                SegmentCount = SegmentCount,
                RecentWarnings = RecentWarnings
            };
    }

    private sealed class IndexWriteLock : IDisposable
    {
        private readonly FileStream stream;
        private readonly string path;

        private IndexWriteLock(FileStream stream, string path)
        {
            this.stream = stream;
            this.path = path;
        }

        public static IndexWriteLock Acquire(string indexRoot)
        {
            var path = Path.Combine(indexRoot, "write.lock");
            while (true)
            {
                try
                {
                    var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
                    return new IndexWriteLock(stream, path);
                }
                catch (IOException)
                {
                    DeleteStaleLock(path);
                    Thread.Sleep(25);
                }
            }
        }

        public void Dispose()
        {
            try
            {
                stream.Dispose();
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        private static void DeleteStaleLock(string path)
        {
            try
            {
                if (File.Exists(path) && DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > TimeSpan.FromMinutes(5))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}

public sealed class IndexUnavailableException : Exception
{
    public IndexUnavailableException(string message) : base(message)
    {
    }
}
