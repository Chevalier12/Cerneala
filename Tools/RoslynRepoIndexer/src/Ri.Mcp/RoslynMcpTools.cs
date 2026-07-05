using System.ComponentModel;
using ModelContextProtocol.Server;
using RoslynRepoIndexer.Core;

namespace Ri.Mcp;

[McpServerToolType]
public sealed class RoslynMcpTools
{
    private readonly IRoslynIndexerApplicationService? applicationService;

    public RoslynMcpTools()
    {
    }

    public RoslynMcpTools(IRoslynIndexerApplicationService applicationService)
        => this.applicationService = applicationService;

    [McpServerTool(Name = "roslyn_doctor", ReadOnly = true, UseStructuredContent = true)]
    [Description("Inspect local repository/index prerequisites and diagnostics.")]
    public async Task<RoslynMcpToolResult<DoctorSummary>> DoctorAsync(RoslynRepoRequest request, CancellationToken cancellationToken = default)
    {
        var app = CreateService(request.RepoRoot);
        return ToToolResult("roslyn_doctor", await app.DoctorAsync(new PathCommandRequest(request.RepoRoot, request.ConfigPath), cancellationToken).ConfigureAwait(false));
    }

    [McpServerTool(Name = "roslyn_index", ReadOnly = false, UseStructuredContent = true)]
    [Description("Build or update the local Roslyn repository index. Writes only under .roslyn-index in the repo root.")]
    public async Task<RoslynMcpToolResult<IndexSummary>> IndexAsync(RoslynIndexRequest request, CancellationToken cancellationToken = default)
    {
        var app = CreateService(request.RepoRoot);
        var response = await app.IndexAsync(
            new IndexCommandRequest(request.RepoRoot, request.Force, request.IncludeGenerated, request.IncludeNonCSharpText ?? false, request.MaxTextFileBytes, request.MaxDegreeOfParallelism, request.ConfigPath),
            cancellationToken).ConfigureAwait(false);
        return ToToolResult("roslyn_index", response);
    }

    [McpServerTool(Name = "roslyn_status", ReadOnly = true, UseStructuredContent = true)]
    [Description("Return deterministic local index status for the repository.")]
    public Task<RoslynMcpToolResult<StatusSummary>> StatusAsync(RoslynRepoRequest request)
    {
        var app = CreateService(request.RepoRoot);
        return Task.FromResult(ToToolResult("roslyn_status", app.Status(new PathCommandRequest(request.RepoRoot, request.ConfigPath))));
    }

    [McpServerTool(Name = "roslyn_search", ReadOnly = true, UseStructuredContent = true)]
    [Description("Search the existing local index for symbols, text, files, or references.")]
    public Task<RoslynMcpToolResult<IReadOnlyList<SearchResult>>> SearchAsync(RoslynSearchRequest request)
    {
        var app = CreateService(request.RepoRoot);
        var response = app.Search(new SearchCommandRequest(
            request.Query,
            ParseMode(request.Mode),
            request.Limit,
            request.Kind,
            request.Path,
            request.Project,
            request.IncludeTests,
            request.FromFile,
            request.FromProject,
            request.TimeoutMs));
        return Task.FromResult(ToToolResult("roslyn_search", response));
    }

    [McpServerTool(Name = "roslyn_read", ReadOnly = true, UseStructuredContent = true)]
    [Description("Return a full local repository file. Prefer roslyn_read before editing so the complete file context is available.")]
    public Task<RoslynMcpToolResult<RepositoryFileReadResult>> ReadAsync(RoslynReadRequest request)
    {
        var app = CreateService(request.RepoRoot);
        return Task.FromResult(ToToolResult("roslyn_read", app.Read(new FileReadCommandRequest(request.FilePath, request.ConfigPath, request.MaxTextFileBytes))));
    }

    [McpServerTool(Name = "roslyn_pread", ReadOnly = true, UseStructuredContent = true)]
    [Description("Return a targeted partial read of a local repository file for focused inspection after full context is known.")]
    public Task<RoslynMcpToolResult<RepositoryPartialFileReadResult>> PReadAsync(RoslynPReadRequest request)
    {
        var app = CreateService(request.RepoRoot);
        return Task.FromResult(ToToolResult("roslyn_pread", app.PartialRead(new PartialFileReadCommandRequest(
            request.FilePath,
            request.StartLine,
            request.EndLine,
            request.AroundLine,
            request.Context,
            request.ConfigPath,
            request.MaxTextFileBytes))));
    }

    [McpServerTool(Name = "roslyn_goto", ReadOnly = true, UseStructuredContent = true)]
    [Description("Find symbol declarations in the local index.")]
    public Task<RoslynMcpToolResult<IReadOnlyList<SearchResult>>> GotoAsync(RoslynGotoRequest request)
    {
        var app = CreateService(request.RepoRoot);
        return Task.FromResult(ToToolResult("roslyn_goto", app.Goto(new SymbolQueryCommandRequest(request.Query, request.Kind, request.Limit))));
    }

    [McpServerTool(Name = "roslyn_refs", ReadOnly = true, UseStructuredContent = true)]
    [Description("Find indexed or exact Roslyn references for a symbol without shell execution or network calls.")]
    public async Task<RoslynMcpToolResult<object>> RefsAsync(RoslynRefsRequest request, CancellationToken cancellationToken = default)
    {
        var app = CreateService(request.RepoRoot);
        return ToToolResult("roslyn_refs", await app.RefsAsync(new RefsCommandRequest(request.Query, request.SymbolId, request.Exact, request.TimeoutSeconds, request.Limit), cancellationToken).ConfigureAwait(false));
    }

    [McpServerTool(Name = "roslyn_suggest", ReadOnly = true, UseStructuredContent = true)]
    [Description("Create deterministic index-backed query suggestions from a natural-language question.")]
    public Task<RoslynMcpToolResult<object>> SuggestAsync(RoslynSuggestRequest request)
    {
        var app = CreateService(request.RepoRoot);
        return Task.FromResult(ToToolResult("roslyn_suggest", app.Suggest(new SuggestCommandRequest(request.Question, request.Limit, request.ExecuteTop))));
    }

    private IRoslynIndexerApplicationService CreateService(string repoRoot)
        => applicationService ?? new RoslynIndexerApplicationService(repoRoot);

    private static RoslynMcpToolResult<T> ToToolResult<T>(string tool, CommandResponse<T> response)
        => new(
            response.Success,
            tool,
            response.RepoRoot,
            response.ElapsedMs ?? 0,
            response.Warnings,
            response.Errors,
            response.Data,
            response.ExitCode,
            response.IndexUpdatedUtc,
            response.Results);

    private static SearchMode ParseMode(string mode)
        => Enum.TryParse<SearchMode>(mode, ignoreCase: true, out var parsed) ? parsed : SearchMode.All;
}
