using System.Text.Json;
using RoslynRepoIndexer.Core;

namespace RoslynRepoIndexer.Tests;

public sealed class JsonContractSnapshotTests
{
    private static readonly DateTimeOffset FixedCreatedUtc = DateTimeOffset.Parse("2026-01-02T03:04:05Z");
    private static readonly DateTimeOffset FixedUpdatedUtc = DateTimeOffset.Parse("2026-01-02T03:05:06Z");

    [Fact]
    public void Manifest_json_serialization_keeps_stable_contract_shape()
    {
        var manifest = IndexManifest.CreateNew("C:/repo", "cfg-hash", "workspace-hash") with
        {
            CreatedUtc = FixedCreatedUtc,
            UpdatedUtc = FixedUpdatedUtc,
            WorkspaceInputs = new[] { new WorkspaceInput("Repo.sln", "solution") },
            DocumentsByRelativePath = new Dictionary<string, DocumentState>(StringComparer.Ordinal)
            {
                ["src/App.cs"] = new("content-hash", 123, FixedUpdatedUtc, true)
            },
            DocumentCount = 1,
            SymbolCount = 2,
            ReferenceCount = 3,
            TokenCount = 4,
            WarningCount = 1,
            RecentWarnings = new[] { "sample warning" }
        };

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(manifest, JsonOptions.Default));
        var root = document.RootElement;

        AssertObjectProperties(root,
            "schemaVersion",
            "generationId",
            "toolVersion",
            "storageFormat",
            "repoRoot",
            "createdUtc",
            "updatedUtc",
            "configHash",
            "workspaceInputsHash",
            "discoveryFingerprint",
            "repositoryStateFingerprint",
            "workspaceInputs",
            "documentsByRelativePath",
            "documentCount",
            "symbolCount",
            "referenceCount",
            "tokenCount",
            "warningCount",
            "segmentCount",
            "segmentsWritten",
            "segmentsReused",
            "segmentBytes",
            "recentWarnings",
            "timings");
        Assert.Equal(IndexManifest.CurrentSchemaVersion, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("Repo.sln", root.GetProperty("workspaceInputs")[0].GetProperty("path").GetString());

        var documentState = root.GetProperty("documentsByRelativePath").GetProperty("src/App.cs");
        AssertObjectProperties(documentState, "contentHash", "length", "lastWriteUtc", "isCSharp");
    }

    [Fact]
    public void Search_result_json_output_schema_keeps_agent_fields()
    {
        var result = SearchResultFixture();

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result, JsonOptions.Default));
        var root = document.RootElement;

        AssertSearchResultShape(root);
        Assert.Equal("src/Services/CustomerService.cs", root.GetProperty("filePath").GetString());
        Assert.Equal(10, root.GetProperty("startLine").GetInt32());
        Assert.Equal(5, root.GetProperty("startColumn").GetInt32());
    }

    [Fact]
    public void Search_json_response_respects_common_contract_and_results_alias()
    {
        var response = CommandResponse.Success(
            new[] { SearchResultFixture() },
            warnings: new[] { "non fatal" },
            command: "ri search CustomerService --json",
            query: "CustomerService",
            repoRoot: "C:/repo",
            elapsedMs: 42,
            indexUpdatedUtc: FixedUpdatedUtc,
            includeResultsAlias: true);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(response, JsonOptions.Default));
        var root = document.RootElement;

        AssertCommonResponseShape(root);
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(0, root.GetProperty("exitCode").GetInt32());
        Assert.Equal("non fatal", root.GetProperty("warnings")[0].GetString());
        Assert.Empty(root.GetProperty("errors").EnumerateArray());
        AssertSearchResultShape(root.GetProperty("data")[0]);
        AssertSearchResultShape(root.GetProperty("results")[0]);
    }

    [Fact]
    public void Command_json_snapshots_keep_common_contract_shape_without_cli()
    {
        var responses = new[]
        {
            JsonSerializer.SerializeToElement(CommandResponse.Success(
                new IndexSummary("C:/repo", 1, 2, 3, 4, 0, TimeSpan.FromMilliseconds(123), FullRebuild: true, Incremental: false, DirtyDocuments: 1, DeletedDocuments: 0, UnchangedDocuments: 0, Timings: new IndexTimingSummary(1, 2, 3, 4, 5, 123)),
                warnings: Array.Empty<string>(),
                command: "ri index . --json",
                query: null,
                repoRoot: "C:/repo",
                elapsedMs: 123,
                indexUpdatedUtc: FixedUpdatedUtc,
                includeResultsAlias: false), JsonOptions.Default),
            JsonSerializer.SerializeToElement(CommandResponse.Success(
                new StatusSummary(IndexStatus.Valid, "C:/repo", IndexManifest.CurrentSchemaVersion, 1, 2, 3, 4, 0, Array.Empty<string>()),
                warnings: Array.Empty<string>(),
                command: "ri status --json",
                query: null,
                repoRoot: "C:/repo",
                elapsedMs: 7,
                indexUpdatedUtc: FixedUpdatedUtc,
                includeResultsAlias: false), JsonOptions.Default),
            JsonSerializer.SerializeToElement(CommandResponse.Success(
                new DoctorSummary("C:/repo", new[] { new DoctorCheck("repo-root", "ok", "info", "Repository root found.", new Dictionary<string, string> { ["kind"] = "git" }) }),
                warnings: Array.Empty<string>(),
                command: "ri doctor . --json",
                query: null,
                repoRoot: "C:/repo",
                elapsedMs: 9,
                indexUpdatedUtc: null,
                includeResultsAlias: false), JsonOptions.Default),
            JsonSerializer.SerializeToElement(CommandResponse.Success(
                new[] { new QuerySuggestion("ri search \"jwt auth\"", "jwt auth", "all", 0.9, "deterministic token search", "mixed") },
                warnings: Array.Empty<string>(),
                command: "ri suggest \"jwt\" --json",
                query: "jwt",
                repoRoot: "C:/repo",
                elapsedMs: 5,
                indexUpdatedUtc: FixedUpdatedUtc,
                includeResultsAlias: true), JsonOptions.Default)
        };

        foreach (var response in responses)
        {
            AssertCommonResponseShape(response);
            Assert.Equal(JsonValueKind.Array, response.GetProperty("warnings").ValueKind);
            Assert.Equal(JsonValueKind.Array, response.GetProperty("errors").ValueKind);
        }

        AssertObjectProperties(responses[0].GetProperty("data"),
            "repoRoot",
            "documents",
            "symbols",
            "references",
            "tokens",
            "warnings",
            "duration",
            "fullRebuild",
            "incremental",
            "dirtyDocuments",
            "deletedDocuments",
            "unchangedDocuments",
            "timings",
            "totalMs");
        Assert.Equal("valid", responses[1].GetProperty("data").GetProperty("indexState").GetString());

        var doctorCheck = responses[2].GetProperty("data").GetProperty("checks")[0];
        AssertObjectProperties(doctorCheck, "name", "status", "severity", "message", "details");

        var suggestion = responses[3].GetProperty("data")[0];
        AssertObjectProperties(suggestion, "command", "query", "mode", "confidence", "reason", "expectedResultKind");
        Assert.Equal(responses[3].GetProperty("data").GetRawText(), responses[3].GetProperty("results").GetRawText());
    }

    private static SearchResult SearchResultFixture()
        => new(
            "src/Services/CustomerService.cs",
            10,
            5,
            10,
            20,
            "symbol",
            900,
            "symbol exact name",
            "public sealed class CustomerService",
            "symbol-id",
            "CustomerService",
            "Services",
            "App.Services.CustomerService",
            "definition",
            "App");

    private static void AssertCommonResponseShape(JsonElement root)
    {
        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        var names = root.EnumerateObject().Select(property => property.Name).ToArray();
        foreach (var required in new[] { "success", "exitCode", "data", "warnings", "errors", "command", "repoRoot", "elapsedMs" })
        {
            Assert.Contains(required, names);
        }
    }

    private static void AssertSearchResultShape(JsonElement root)
        => AssertObjectProperties(root,
            "path",
            "line",
            "column",
            "endLine",
            "endColumn",
            "kind",
            "score",
            "matchReason",
            "snippet",
            "symbolId",
            "symbolName",
            "containingType",
            "fullyQualifiedName",
            "referenceKind",
            "projectName",
            "filePath",
            "startLine",
            "startColumn");

    private static void AssertObjectProperties(JsonElement element, params string[] expected)
    {
        Assert.Equal(JsonValueKind.Object, element.ValueKind);
        Assert.Equal(expected, element.EnumerateObject().Select(property => property.Name).ToArray());
    }
}
