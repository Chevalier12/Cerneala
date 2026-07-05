using System.Text.Json.Serialization;

namespace Ri.Mcp;

public sealed record RoslynMcpToolDefinition(string Name, string Description, string InputSchemaJson);

public sealed record RoslynMcpToolResult<T>(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("tool")] string Tool,
    [property: JsonPropertyName("repoRoot")] string? RepoRoot,
    [property: JsonPropertyName("elapsedMs")] long ElapsedMs,
    [property: JsonPropertyName("warnings")] IReadOnlyList<string> Warnings,
    [property: JsonPropertyName("errors")] IReadOnlyList<string> Errors,
    [property: JsonPropertyName("data")] T? Data,
    [property: JsonPropertyName("exitCode")] int ExitCode = 0,
    [property: JsonPropertyName("indexUpdatedUtc")] DateTimeOffset? IndexUpdatedUtc = null,
    [property: JsonPropertyName("results")] T? Results = default);

public sealed record RoslynRepoRequest(
    string RepoRoot,
    string? ConfigPath = null);

public sealed record RoslynIndexRequest(
    string RepoRoot,
    bool Force = false,
    bool IncludeGenerated = false,
    bool? IncludeNonCSharpText = null,
    long? MaxTextFileBytes = null,
    int? MaxDegreeOfParallelism = null,
    string? ConfigPath = null);

public sealed record RoslynSearchRequest(
    string RepoRoot,
    string Query,
    string Mode = "all",
    int Limit = 50,
    string? Kind = null,
    string? Path = null,
    string? Project = null,
    bool? IncludeTests = null,
    string? FromFile = null,
    string? FromProject = null,
    int? TimeoutMs = null);

public sealed record RoslynReadRequest(
    string RepoRoot,
    string FilePath,
    string? ConfigPath = null,
    long? MaxTextFileBytes = null);

public sealed record RoslynPReadRequest(
    string RepoRoot,
    string FilePath,
    int? StartLine = null,
    int? EndLine = null,
    int? AroundLine = null,
    int Context = 40,
    string? ConfigPath = null,
    long? MaxTextFileBytes = null);

public sealed record RoslynGotoRequest(
    string RepoRoot,
    string Query,
    string? Kind = null,
    int Limit = 20);

public sealed record RoslynRefsRequest(
    string RepoRoot,
    string Query,
    string? SymbolId = null,
    bool Exact = false,
    int? TimeoutSeconds = null,
    int Limit = 50);

public sealed record RoslynSuggestRequest(
    string RepoRoot,
    string Question,
    int Limit = 5,
    int ExecuteTop = 0);
