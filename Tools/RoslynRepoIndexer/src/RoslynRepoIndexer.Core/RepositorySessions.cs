using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

namespace RoslynRepoIndexer.Core;

public sealed class QueryIndex
{
    public QueryIndex(IndexSnapshot snapshot)
    {
        Snapshot = snapshot;
        SymbolsById = snapshot.Symbols.GroupBy(x => x.SymbolId, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
        SymbolsByLowerName = Group(snapshot.Symbols, x => x.Name.ToLowerInvariant());
        SymbolsByLowerFullyQualifiedName = Group(snapshot.Symbols, x => x.FullyQualifiedName.ToLowerInvariant());
        SymbolsByTerm = GroupMany(snapshot.Symbols, symbol => Tokenizer.NormalizeTerms(symbol.Name + " " + symbol.FullyQualifiedName));
        TokenToPostings = Group(snapshot.Tokens, x => x.Token);
        ReferencesBySymbolId = Group(snapshot.References, x => x.SymbolId);
        DocumentsById = snapshot.Documents.GroupBy(x => x.DocumentId, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
        DocumentsByPath = snapshot.Documents.GroupBy(x => x.RelativePath, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
    }

    public IndexSnapshot Snapshot { get; }
    internal IReadOnlyDictionary<string, SymbolEntry> SymbolsById { get; }
    internal IReadOnlyDictionary<string, IReadOnlyList<SymbolEntry>> SymbolsByLowerName { get; }
    internal IReadOnlyDictionary<string, IReadOnlyList<SymbolEntry>> SymbolsByLowerFullyQualifiedName { get; }
    internal IReadOnlyDictionary<string, IReadOnlyList<SymbolEntry>> SymbolsByTerm { get; }
    internal IReadOnlyDictionary<string, IReadOnlyList<TokenPosting>> TokenToPostings { get; }
    internal IReadOnlyDictionary<string, IReadOnlyList<ReferenceEntry>> ReferencesBySymbolId { get; }
    internal IReadOnlyDictionary<string, DocumentEntry> DocumentsById { get; }
    internal IReadOnlyDictionary<string, DocumentEntry> DocumentsByPath { get; }

    private static IReadOnlyDictionary<string, IReadOnlyList<T>> Group<T>(IEnumerable<T> values, Func<T, string> keySelector)
        => values.GroupBy(keySelector, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<T>)x.ToArray(), StringComparer.Ordinal);

    private static IReadOnlyDictionary<string, IReadOnlyList<T>> GroupMany<T>(IEnumerable<T> values, Func<T, IEnumerable<string>> keys)
        => values.SelectMany(value => keys(value).Distinct(StringComparer.Ordinal).Select(key => (key, value)))
            .GroupBy(item => item.key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<T>)group.Select(item => item.value).ToArray(), StringComparer.Ordinal);
}

public sealed record RepositorySessionMetrics(long SessionHits, long ReloadCount, long LoadMs, long QueryCount, long TotalQueryMs);

public sealed class RepositoryIndexSession : IDisposable
{
    private readonly string repoRoot;
    private readonly SemaphoreSlim reloadGate = new(1, 1);
    private QueryIndex? current;
    private IndexGenerationStamp? currentStamp;
    private long sessionHits;
    private long reloadCount;
    private long loadMs;
    private long queryCount;
    private long totalQueryMs;
    private readonly RepositoryWorkspaceSession workspaceSession = new();

    public RepositoryIndexSession(string repoRoot)
        => this.repoRoot = Path.GetFullPath(repoRoot);

    public async ValueTask<QueryIndex> GetQueryIndexAsync(CancellationToken cancellationToken = default)
    {
        var stamp = IndexStore.GetGenerationStamp(repoRoot);
        var captured = Volatile.Read(ref current);
        if (captured is not null && stamp == Volatile.Read(ref currentStamp))
        {
            Interlocked.Increment(ref sessionHits);
            return captured;
        }

        await reloadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            stamp = IndexStore.GetGenerationStamp(repoRoot);
            captured = Volatile.Read(ref current);
            if (captured is not null && stamp == Volatile.Read(ref currentStamp))
            {
                Interlocked.Increment(ref sessionHits);
                return captured;
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var loaded = new QueryIndex(IndexStore.Read(repoRoot));
            stopwatch.Stop();
            Volatile.Write(ref currentStamp, stamp);
            Volatile.Write(ref current, loaded);
            Interlocked.Increment(ref reloadCount);
            Interlocked.Exchange(ref loadMs, stopwatch.ElapsedMilliseconds);
            return loaded;
        }
        finally
        {
            reloadGate.Release();
        }
    }

    public QueryIndex GetQueryIndex() => GetQueryIndexAsync().AsTask().GetAwaiter().GetResult();

    public void RecordQuery(long elapsedMs)
    {
        Interlocked.Increment(ref queryCount);
        Interlocked.Add(ref totalQueryMs, Math.Max(0, elapsedMs));
    }

    public RepositorySessionMetrics Metrics => new(
        Interlocked.Read(ref sessionHits),
        Interlocked.Read(ref reloadCount),
        Interlocked.Read(ref loadMs),
        Interlocked.Read(ref queryCount),
        Interlocked.Read(ref totalQueryMs));

    public IndexBuilder CreateIndexBuilder() => new(workspaceSession);
    public string SessionState => Volatile.Read(ref current) is null ? "cold" : "loaded";
    public string WorkspaceState => workspaceSession.IsLoaded ? "loaded" : "not-loaded";

    public void Dispose()
    {
        reloadGate.Dispose();
        workspaceSession.Dispose();
    }
}

public sealed class RepositoryWorkspaceSession : IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Dictionary<string, CachedWorkspace> workspaces = new(StringComparer.OrdinalIgnoreCase);
    private int loaded;
    public bool IsLoaded => Volatile.Read(ref loaded) != 0;

    internal async Task<Solution> LoadSolutionAsync(WorkspaceInput input, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var path = Path.GetFullPath(input.Path);
            if (!workspaces.TryGetValue(path, out var cached) || HasWorkspaceShapeChanged(cached))
            {
                cached?.Dispose();
                cached = await OpenAsync(input with { Path = path }, cancellationToken).ConfigureAwait(false);
                workspaces[path] = cached;
                Volatile.Write(ref loaded, 1);
                return cached.Solution;
            }

            var solution = cached.Solution;
            foreach (var document in solution.Projects.SelectMany(project => project.Documents))
            {
                if (document.FilePath is null || !File.Exists(document.FilePath)) continue;
                var info = new FileInfo(document.FilePath);
                var state = new FileState(info.Length, info.LastWriteTimeUtc.Ticks);
                if (cached.DocumentStates.TryGetValue(document.Id, out var previous) && previous == state) continue;
                var text = SourceText.From(await File.ReadAllTextAsync(document.FilePath, cancellationToken).ConfigureAwait(false), Encoding.UTF8);
                solution = solution.WithDocumentText(document.Id, text, PreservationMode.PreserveIdentity);
                cached.DocumentStates[document.Id] = state;
            }

            cached.Solution = solution;
            return solution;
        }
        finally
        {
            gate.Release();
        }
    }

    public void Dispose()
    {
        foreach (var cached in workspaces.Values) cached.Dispose();
        workspaces.Clear();
        Volatile.Write(ref loaded, 0);
        gate.Dispose();
    }

    private static async Task<CachedWorkspace> OpenAsync(WorkspaceInput input, CancellationToken cancellationToken)
    {
        MSBuildRegistration.RegisterDefaults();
        var workspace = IndexBuilder.CreateWorkspace();
        var solution = input.Kind.Equals("csproj", StringComparison.OrdinalIgnoreCase)
            ? IndexBuilder.RemoveAnalyzerReferences(await workspace.OpenProjectAsync(input.Path, cancellationToken: cancellationToken).ConfigureAwait(false)).Solution
            : IndexBuilder.RemoveAnalyzerReferences(await workspace.OpenSolutionAsync(input.Path, cancellationToken: cancellationToken).ConfigureAwait(false));
        return new CachedWorkspace(workspace, solution, GetFileState(input.Path), SourcePaths(solution), DocumentStates(solution));
    }

    private static bool HasWorkspaceShapeChanged(CachedWorkspace cached)
        => cached.InputState != GetFileState(cached.InputPath) ||
           cached.Solution.Projects.SelectMany(project => project.Documents).Any(document => document.FilePath is not null && !File.Exists(document.FilePath)) ||
           HasNewSourceFile(cached);

    private static FileState GetFileState(string path)
    {
        var info = new FileInfo(path);
        return new FileState(info.Exists ? info.Length : -1, info.Exists ? info.LastWriteTimeUtc.Ticks : 0);
    }

    private static Dictionary<DocumentId, FileState> DocumentStates(Solution solution)
        => solution.Projects.SelectMany(project => project.Documents)
            .Where(document => document.FilePath is not null && File.Exists(document.FilePath))
            .ToDictionary(document => document.Id, document => GetFileState(document.FilePath!));

    private static HashSet<string> SourcePaths(Solution solution)
        => solution.Projects.SelectMany(project => project.Documents)
            .Where(document => document.FilePath is not null)
            .Select(document => Path.GetFullPath(document.FilePath!))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static bool HasNewSourceFile(CachedWorkspace cached)
    {
        foreach (var projectDirectory in cached.Solution.Projects
                     .Select(project => Path.GetDirectoryName(project.FilePath))
                     .Where(directory => directory is not null)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                foreach (var path in Directory.EnumerateFiles(projectDirectory!, "*.cs", SearchOption.AllDirectories))
                {
                    var relative = Path.GetRelativePath(projectDirectory!, path);
                    if (relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                            .Any(part => part.Equals("bin", StringComparison.OrdinalIgnoreCase) || part.Equals("obj", StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    if (!cached.SourcePaths.Contains(Path.GetFullPath(path))) return true;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return true;
            }
        }

        return false;
    }

    private readonly record struct FileState(long Length, long LastWriteTicks);

    private sealed class CachedWorkspace : IDisposable
    {
        public CachedWorkspace(MSBuildWorkspace workspace, Solution solution, FileState inputState, HashSet<string> sourcePaths, Dictionary<DocumentId, FileState> documentStates)
        {
            Workspace = workspace;
            Solution = solution;
            InputPath = solution.FilePath ?? solution.Projects.FirstOrDefault()?.FilePath ?? string.Empty;
            InputState = inputState;
            SourcePaths = sourcePaths;
            DocumentStates = documentStates;
        }
        public MSBuildWorkspace Workspace { get; }
        public Solution Solution { get; set; }
        public string InputPath { get; }
        public FileState InputState { get; }
        public HashSet<string> SourcePaths { get; }
        public Dictionary<DocumentId, FileState> DocumentStates { get; }
        public void Dispose() => Workspace.Dispose();
    }
}

public sealed class RepositorySessionRegistry
{
    private readonly ConcurrentDictionary<string, RepositoryIndexSession> sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, long> lastAccess = new(StringComparer.OrdinalIgnoreCase);
    private readonly object evictionGate = new();
    private readonly int maxSessions;

    public RepositorySessionRegistry(int maxSessions = 4)
    {
        if (maxSessions < 1) throw new ArgumentOutOfRangeException(nameof(maxSessions));
        this.maxSessions = maxSessions;
    }

    public RepositoryIndexSession Get(string repoRoot)
    {
        var normalized = Path.GetFullPath(repoRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var session = sessions.GetOrAdd(normalized, static root => new RepositoryIndexSession(root));
        lastAccess[normalized] = Stopwatch.GetTimestamp();
        EvictIfNeeded(normalized);
        return session;
    }

    public int Count => sessions.Count;

    public bool Contains(string repoRoot)
        => sessions.ContainsKey(Path.GetFullPath(repoRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    private void EvictIfNeeded(string current)
    {
        if (sessions.Count <= maxSessions) return;
        lock (evictionGate)
        {
            while (sessions.Count > maxSessions)
            {
                var candidate = lastAccess.Where(pair => !string.Equals(pair.Key, current, StringComparison.OrdinalIgnoreCase)).OrderBy(pair => pair.Value).ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
                if (candidate.Key is null) return;
                if (sessions.TryRemove(candidate.Key, out var removed)) removed.Dispose();
                lastAccess.TryRemove(candidate.Key, out _);
            }
        }
    }
}

public sealed record IndexGenerationStamp(string GenerationId, DateTimeOffset UpdatedUtc, long ManifestLength);
