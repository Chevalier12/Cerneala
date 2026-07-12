using System.Text.Json.Serialization;

namespace RoslynRepoIndexer.Core;

public enum RepositoryRootKind { Git, WorkspaceFile }
public enum SearchMode { All, Symbol, Text, File, Reference }
public enum IndexStatus { Missing, Valid, Stale, Corrupt, SchemaIncompatible }

public sealed record RepositoryRoot(string RootPath, RepositoryRootKind Kind);
public sealed record CandidateFile(string FullPath, string RelativePath, long Length, DateTime LastWriteUtc = default);
public sealed record ConfigLoadResult(IndexerConfig Config, IReadOnlyList<string> Warnings);
public sealed record WorkspaceInput(string Path, string Kind);
public sealed record TokenValue(string Value, int Line, int Column);
public sealed record ParsedQuery(IReadOnlyList<string> Terms, IReadOnlyList<string> Phrases, IReadOnlyDictionary<string, string> Filters);
public sealed record SearchRequest(
    string Query,
    SearchMode Mode = SearchMode.All,
    int Limit = 50,
    string? Kind = null,
    string? Path = null,
    string? Project = null,
    bool? IncludeTests = null,
    string? FromFile = null,
    string? FromProject = null,
    int? TimeoutMs = null);

public sealed record ProjectEntry(string ProjectId, string Name, string? FilePath, string Language, string? TargetFramework);

public sealed record DocumentEntry(
    string DocumentId,
    string? ProjectId,
    string RelativePath,
    string? ProjectName,
    string Language,
    bool IsCSharp,
    bool IsGenerated,
    bool IsNonCSharpText,
    long LengthBytes,
    DateTimeOffset LastWriteUtc,
    string ContentHash,
    string DeclarationHash,
    int LineCount);

public sealed record DocumentState(string ContentHash, long Length, DateTimeOffset LastWriteUtc, bool IsCSharp);

public sealed record SymbolEntry(
    string SymbolId,
    string DocumentId,
    string? ProjectId,
    string Kind,
    string Name,
    string MetadataName,
    string FullyQualifiedName,
    string? ContainerName,
    string Signature,
    string Accessibility,
    IReadOnlyList<string> Modifiers,
    string Path,
    int Line,
    int Column,
    int EndLine,
    int EndColumn,
    int SpanStart,
    int SpanLength,
    bool IsDefinition,
    bool IsPartial,
    IReadOnlyList<string> ParameterTypes,
    string? ReturnType,
    string? ProjectName,
    string? SymbolKey,
    IReadOnlyList<string>? BaseTypeIds = null,
    IReadOnlyList<string>? InterfaceTypeIds = null,
    string? OverriddenSymbolId = null)
{
    public IReadOnlyList<string> BaseTypeIds { get; init; } = BaseTypeIds ?? Array.Empty<string>();
    public IReadOnlyList<string> InterfaceTypeIds { get; init; } = InterfaceTypeIds ?? Array.Empty<string>();
}

public sealed record ReferenceEntry(
    string ReferenceId,
    string SymbolId,
    string DocumentId,
    string? ProjectId,
    string ReferencedName,
    string Path,
    int Line,
    int Column,
    int EndLine,
    int EndColumn,
    int SpanStart,
    int SpanLength,
    string? ProjectName,
    string ReferenceKind,
    string? ContainingSymbolId = null,
    bool IsInvocation = false);

public sealed record TokenPosting(string Token, string Path, int Line, int Column, string Field, string Weight, string? ProjectName, string? DocumentId = null);

public sealed record SearchResult(
    string Path,
    int Line,
    int Column,
    int EndLine,
    int EndColumn,
    string Kind,
    double Score,
    string MatchReason,
    string Snippet,
    string? SymbolId = null,
    string? SymbolName = null,
    string? ContainingType = null,
    string? FullyQualifiedName = null,
    string? ReferenceKind = null,
    string? ProjectName = null)
{
    public string FilePath => Path;
    public int StartLine => Line;
    public int StartColumn => Column;
}

public sealed record SearchExecution(
    IReadOnlyList<SearchResult> Results,
    bool TimedOut,
    long SearchLoadMs,
    long SearchScoreMs);

public sealed record RepositoryFileReadResult(
    string FilePath,
    string RepoRoot,
    string Language,
    int LineCount,
    long SizeBytes,
    string ContentHash,
    DateTimeOffset LastModifiedUtc,
    bool IsIndexed,
    string Content);

public sealed record RepositoryPartialFileReadResult(
    string FilePath,
    string RepoRoot,
    string Language,
    int LineCount,
    long SizeBytes,
    string ContentHash,
    DateTimeOffset LastModifiedUtc,
    bool IsIndexed,
    string SelectionMode,
    int StartLine,
    int EndLine,
    int SelectedLineCount,
    string Content,
    int? TargetLine = null,
    int? Context = null);

public sealed record QuerySuggestion(
    string Command,
    string Query,
    string Mode,
    double Confidence,
    string Reason,
    string ExpectedResultKind);

public sealed record IndexDiagnostics(IReadOnlyList<string> Warnings, IReadOnlyList<string> Errors);

public sealed record IndexTimingSummary(
    long DiscoveryMs,
    long WorkspaceLoadMs,
    long SemanticIndexMs,
    long TextIndexMs,
    long PersistMs,
    long TotalMs);

public sealed record IndexSnapshot(
    IndexManifest Manifest,
    IReadOnlyList<DocumentEntry> Documents,
    IReadOnlyList<SymbolEntry> Symbols,
    IReadOnlyList<ReferenceEntry> References,
    IReadOnlyList<TokenPosting> Tokens);

public sealed record IndexManifest
{
    public const int CurrentSchemaVersion = 6;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public string GenerationId { get; init; } = string.Empty;
    public string ToolVersion { get; init; } = "0.1.0";
    public string StorageFormat { get; init; } = "segmented-binary-v1";
    public string RepoRoot { get; init; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; init; }
    public DateTimeOffset UpdatedUtc { get; init; }
    public string ConfigHash { get; init; } = string.Empty;
    public string WorkspaceInputsHash { get; init; } = string.Empty;
    public string DiscoveryFingerprint { get; init; } = string.Empty;
    public string RepositoryStateFingerprint { get; init; } = string.Empty;
    public IReadOnlyList<WorkspaceInput> WorkspaceInputs { get; init; } = Array.Empty<WorkspaceInput>();
    public IReadOnlyDictionary<string, DocumentState> DocumentsByRelativePath { get; init; } = new Dictionary<string, DocumentState>(StringComparer.Ordinal);
    public int DocumentCount { get; init; }
    public int SymbolCount { get; init; }
    public int ReferenceCount { get; init; }
    public int TokenCount { get; init; }
    public int WarningCount { get; init; }
    public int SegmentCount { get; init; }
    public int SegmentsWritten { get; init; }
    public int SegmentsReused { get; init; }
    public long SegmentBytes { get; init; }
    public IReadOnlyList<string> RecentWarnings { get; init; } = Array.Empty<string>();
    public IndexTimingSummary Timings { get; init; } = new(0, 0, 0, 0, 0, 0);

    public static IndexManifest CreateNew(string repoRoot, string configHash, string workspaceInputsHash)
    {
        var now = DateTimeOffset.UtcNow;
        return new IndexManifest
        {
            GenerationId = Guid.NewGuid().ToString("N"),
            RepoRoot = repoRoot,
            CreatedUtc = now,
            UpdatedUtc = now,
            ConfigHash = configHash,
            WorkspaceInputsHash = workspaceInputsHash
        };
    }
}

public sealed record CommandResponse<T>(
    [property: JsonPropertyName("success")]
    bool Success,
    [property: JsonPropertyName("exitCode")]
    int ExitCode,
    [property: JsonPropertyName("data")]
    T? Data,
    [property: JsonPropertyName("warnings")]
    IReadOnlyList<string> Warnings,
    [property: JsonPropertyName("errors")]
    IReadOnlyList<string> Errors,
    [property: JsonPropertyName("command")]
    string? Command = null,
    [property: JsonPropertyName("query")]
    string? Query = null,
    [property: JsonPropertyName("repoRoot")]
    string? RepoRoot = null,
    [property: JsonPropertyName("elapsedMs")]
    long? ElapsedMs = null,
    [property: JsonPropertyName("indexUpdatedUtc")]
    DateTimeOffset? IndexUpdatedUtc = null,
    [property: JsonPropertyName("results")]
    T? Results = default)
{
    public static CommandResponse<T> SuccessResponse(
        T data,
        IReadOnlyList<string>? warnings = null,
        string? command = null,
        string? query = null,
        string? repoRoot = null,
        long? elapsedMs = null,
        DateTimeOffset? indexUpdatedUtc = null,
        bool includeResultsAlias = false)
        => new(true, 0, data, warnings ?? Array.Empty<string>(), Array.Empty<string>(), command, query, repoRoot, elapsedMs, indexUpdatedUtc, includeResultsAlias ? data : default);

    public static CommandResponse<T> Failure(
        int exitCode,
        IReadOnlyList<string> errors,
        IReadOnlyList<string>? warnings = null,
        string? command = null,
        string? query = null,
        string? repoRoot = null,
        long? elapsedMs = null)
        => new(false, exitCode, default, warnings ?? Array.Empty<string>(), errors, command, query, repoRoot, elapsedMs);
}

public static class CommandResponse
{
    public static CommandResponse<T> Success<T>(T data, IReadOnlyList<string>? warnings = null)
        => CommandResponse<T>.SuccessResponse(data, warnings);

    public static CommandResponse<T> Success<T>(
        T data,
        IReadOnlyList<string>? warnings,
        string? command,
        string? query,
        string? repoRoot,
        long? elapsedMs,
        DateTimeOffset? indexUpdatedUtc,
        bool includeResultsAlias)
        => CommandResponse<T>.SuccessResponse(data, warnings, command, query, repoRoot, elapsedMs, indexUpdatedUtc, includeResultsAlias);
}

public sealed record IndexSummary(
    string RepoRoot,
    int Documents,
    int Symbols,
    int References,
    int Tokens,
    int Warnings,
    TimeSpan Duration,
    bool FullRebuild,
    bool Incremental,
    int DirtyDocuments,
    int DeletedDocuments,
    int UnchangedDocuments,
    IndexTimingSummary Timings)
{
    public IndexSummary(
        string repoRoot,
        int documents,
        int symbols,
        int references,
        int tokens,
        int warnings,
        TimeSpan duration,
        bool fullRebuild,
        bool incremental,
        int dirtyDocuments,
        int deletedDocuments,
        int unchangedDocuments)
        : this(repoRoot, documents, symbols, references, tokens, warnings, duration, fullRebuild, incremental, dirtyDocuments, deletedDocuments, unchangedDocuments, new IndexTimingSummary(0, 0, 0, 0, 0, (long)duration.TotalMilliseconds))
    {
    }

    public long TotalMs => Timings.TotalMs;
}
public sealed record StatusSummary(IndexStatus Status, string? RepoRoot, int SchemaVersion, int Documents, int Symbols, int References, int Tokens, int DirtyFiles, IReadOnlyList<string> Warnings)
{
    [property: JsonPropertyName("indexState")]
    public string IndexState => Status switch
    {
        IndexStatus.Missing => "missing",
        IndexStatus.Valid => "valid",
        IndexStatus.Stale => "stale",
        IndexStatus.Corrupt => "corrupt",
        IndexStatus.SchemaIncompatible => "schema-incompatible",
        _ => Status.ToString().ToLowerInvariant()
    };
    public string SessionState { get; init; } = "not-loaded";
    public string WorkspaceState { get; init; } = "not-loaded";
}
public sealed record DoctorCheck(string Name, string Status, string Severity, string Message, IReadOnlyDictionary<string, string>? Details = null)
{
    public IReadOnlyDictionary<string, string> Details { get; init; } = Details ?? new Dictionary<string, string>(StringComparer.Ordinal);
}
public sealed record DoctorSummary(string RepoRoot, IReadOnlyList<DoctorCheck> Checks);

public sealed class WorkspaceLoadingException : Exception
{
    public WorkspaceLoadingException(string message) : base(message)
    {
    }
}

public sealed class NoCSharpDocumentsException : Exception
{
    public NoCSharpDocumentsException(string message) : base(message)
    {
    }
}

public sealed class RepositoryFileReadException : Exception
{
    public RepositoryFileReadException(string message, int exitCode = 2) : base(message)
    {
        ExitCode = exitCode;
    }

    public int ExitCode { get; }
}
