using System.Text.Json.Serialization;
using RoslynRepoIndexer.Core;

namespace Ri.Mcp;

public sealed record RoslynMcpToolDefinition(string Name, string Description, string InputSchemaJson);

public enum RoslynResponseProfile { Compact, Standard, Diagnostic }

public sealed record RoslynMcpError(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("retryable")] bool Retryable,
    [property: JsonPropertyName("suggestedAction")] string? SuggestedAction = null);

public sealed record RoslynMcpCacheInfo(
    [property: JsonPropertyName("sessionHit")] bool SessionHit,
    [property: JsonPropertyName("generationReloaded")] bool GenerationReloaded,
    [property: JsonPropertyName("reloadCount")] long ReloadCount,
    [property: JsonPropertyName("loadMs")] long LoadMs);

public sealed record RoslynMcpToolResult<T>(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("tool")] string Tool,
    [property: JsonPropertyName("repoRoot")] string? RepoRoot,
    [property: JsonPropertyName("elapsedMs")] long ElapsedMs,
    [property: JsonPropertyName("warnings")] IReadOnlyList<string> Warnings,
    [property: JsonPropertyName("errors")] IReadOnlyList<RoslynMcpError> Errors,
    [property: JsonPropertyName("data")] T? Data,
    [property: JsonPropertyName("exitCode")] int ExitCode = 0,
    [property: JsonPropertyName("indexUpdatedUtc")] DateTimeOffset? IndexUpdatedUtc = null,
    [property: JsonPropertyName("cache")] RoslynMcpCacheInfo? Cache = null,
    [property: JsonPropertyName("truncated")] bool Truncated = false,
    [property: JsonPropertyName("continuationToken")] string? ContinuationToken = null);

public sealed record RoslynRepoRequest(
    string? RepoRoot = null,
    string? ConfigPath = null,
    bool Deep = false);

public sealed record RoslynIndexRequest(
    string? RepoRoot = null,
    bool Force = false,
    bool IncludeGenerated = false,
    bool? IncludeNonCSharpText = null,
    long? MaxTextFileBytes = null,
    int? MaxDegreeOfParallelism = null,
    string? ConfigPath = null);

public sealed record RoslynSearchRequest(
    string? RepoRoot = null,
    string Query = "",
    string Mode = "all",
    int Limit = 50,
    string? Kind = null,
    string? Path = null,
    string? Project = null,
    bool? IncludeTests = null,
    string? FromFile = null,
    string? FromProject = null,
    int? TimeoutMs = null,
    RoslynResponseProfile Profile = RoslynResponseProfile.Compact,
    string? ContinuationToken = null);

public sealed record RoslynReadRequest(
    string? RepoRoot = null,
    string FilePath = "",
    string? ConfigPath = null,
    long? MaxTextFileBytes = null,
    int MaxChars = 30_000);

public sealed record RoslynPReadRequest(
    string? RepoRoot = null,
    string FilePath = "",
    int? StartLine = null,
    int? EndLine = null,
    int? AroundLine = null,
    int Context = 40,
    string? ConfigPath = null,
    long? MaxTextFileBytes = null,
    int MaxChars = 30_000);

public sealed record RoslynGotoRequest(
    string? RepoRoot = null,
    string Query = "",
    string? SymbolId = null,
    string? Kind = null,
    int Limit = 20,
    RoslynResponseProfile Profile = RoslynResponseProfile.Compact,
    string? ContinuationToken = null);

public sealed record RoslynRefsRequest(
    string? RepoRoot = null,
    string Query = "",
    string? SymbolId = null,
    bool Exact = false,
    int? TimeoutSeconds = null,
    int Limit = 50,
    RoslynResponseProfile Profile = RoslynResponseProfile.Compact,
    string? ContinuationToken = null);

public sealed record RoslynSuggestRequest(
    string? RepoRoot = null,
    string Question = "",
    int Limit = 5,
    int ExecuteTop = 0);

public sealed record RoslynOutlineRequest(
    string? RepoRoot = null,
    string Target = "",
    int Depth = 2,
    int MaxResults = 200,
    int MaxChars = 30_000,
    bool IncludePrivate = false,
    bool IncludeGenerated = false);

public sealed record RoslynInspectRequest(
    string? RepoRoot = null,
    string Symbol = "",
    IReadOnlyList<InspectInclude>? Include = null,
    int Depth = 1,
    int MaxResults = 80,
    int MaxChars = 30_000);

public sealed record RoslynContextRequest(
    string? RepoRoot = null,
    string Symbol = "",
    int MaxResults = 40,
    int MaxChars = 30_000);

public sealed record RoslynCallGraphRequest(
    string? RepoRoot = null,
    string Symbol = "",
    CallGraphDirection Direction = CallGraphDirection.Both,
    int Depth = 1,
    int MaxNodes = 100,
    bool IncludeTests = true,
    bool IncludeExternal = false);

public sealed record RoslynImpactRequest(
    string? RepoRoot = null,
    string Symbol = "",
    int MaxResults = 100);

public sealed record RoslynTestsForRequest(
    string? RepoRoot = null,
    string Symbol = "",
    int MaxResults = 50);

public enum RoslynBatchFailureMode { Stop, Continue }

public sealed record RoslynBatchOperation(
    string Id,
    string Operation,
    string? Query = null,
    string? SymbolFrom = null,
    string? FileFrom = null,
    IReadOnlyList<InspectInclude>? Include = null,
    int Depth = 1,
    int Limit = 50);

public sealed record RoslynBatchRequest(
    string? RepoRoot = null,
    IReadOnlyList<RoslynBatchOperation>? Operations = null,
    RoslynBatchFailureMode FailureMode = RoslynBatchFailureMode.Stop,
    int MaxChars = 40_000,
    int TimeoutMs = 1_000);

public sealed record RoslynBatchOperationResult(string Id, string Operation, bool Success, object? Data, RoslynMcpError? Error = null);
public sealed record RoslynBatchResult(string GenerationId, IReadOnlyList<RoslynBatchOperationResult> Operations, bool Truncated, bool TimedOut);

public sealed record RoslynChangesRequest(
    string? RepoRoot = null,
    ChangeComparison Comparison = ChangeComparison.PreviousGeneration,
    string? BaseId = null,
    string? TargetId = null,
    int MaxResults = 500);

public sealed record RoslynProfileRequest(string? RepoRoot = null, int TopTerms = 20);
public sealed record RoslynProfileFileSize(string Name, long Bytes);
public sealed record RoslynProfileTerm(string Term, int PostingCount);
public sealed record RoslynProfileResult(
    string GenerationId,
    RepositorySessionMetrics Session,
    IndexTimingSummary IndexTimings,
    IReadOnlyList<RoslynProfileFileSize> Files,
    IReadOnlyList<RoslynProfileTerm> TopTerms,
    long TotalIndexBytes);

public sealed record RoslynCapabilities(
    string ServerVersion,
    int IndexSchemaVersion,
    string ContractVersion,
    string? BoundRepository,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> ResponseProfiles,
    IReadOnlyDictionary<string, int> Limits);
