using System.Text.Json;
using RoslynRepoIndexer.Core;

namespace Ri.Mcp;

public static class RoslynMcpToolCatalog
{
    public static IReadOnlyList<RoslynMcpToolDefinition> Tools { get; } =
    [
        Tool("roslyn_doctor", "Inspect local repository/index prerequisites and diagnostics.", [], "repoRoot", "configPath", "deep"),
        Tool("roslyn_index", "Build or update the local Roslyn repository index. Writes only under .roslyn-index in the repo root.", [], "repoRoot", "force", "includeGenerated", "includeNonCSharpText", "maxTextFileBytes", "maxDegreeOfParallelism", "configPath"),
        Tool("roslyn_status", "Return deterministic local index status for the repository.", [], "repoRoot", "configPath"),
        Tool("roslyn_search", "Search the persistent local index for symbols, text, files, or references.", ["query"], "repoRoot", "query", "mode", "limit", "kind", "path", "project", "includeTests", "fromFile", "fromProject", "timeoutMs", "profile", "continuationToken"),
        Tool("roslyn_read", "Return a full local repository file. Prefer roslyn_read before editing so the complete file context is available.", ["filePath"], "repoRoot", "filePath", "configPath", "maxTextFileBytes", "maxChars"),
        PReadTool(),
        Tool("roslyn_goto", "Find symbol declarations in the persistent local index by symbol ID or query.", [], "repoRoot", "query", "symbolId", "kind", "limit", "profile", "continuationToken"),
        Tool("roslyn_refs", "Find indexed or exact Roslyn references for a symbol without shell execution or network calls.", [], "repoRoot", "query", "symbolId", "exact", "timeoutSeconds", "limit", "profile", "continuationToken"),
        Tool("roslyn_outline", "Return a bounded semantic outline for a file, type, or namespace.", ["target"], "repoRoot", "target", "depth", "maxResults", "maxChars", "includePrivate", "includeGenerated"),
        Tool("roslyn_inspect", "Resolve one symbol strictly and return selected source, relationship, reference, and test context.", ["symbol"], "repoRoot", "symbol", "include", "depth", "maxResults", "maxChars"),
        Tool("roslyn_context", "Build a compact relevance-ranked context package for one symbol.", ["symbol"], "repoRoot", "symbol", "maxResults", "maxChars"),
        Tool("roslyn_callgraph", "Traverse the indexed invocation graph with deterministic depth and node bounds.", ["symbol"], "repoRoot", "symbol", "direction", "depth", "maxNodes", "includeTests", "includeExternal"),
        Tool("roslyn_impact", "Return demonstrable semantic and structural impact relationships for one symbol.", ["symbol"], "repoRoot", "symbol", "maxResults"),
        Tool("roslyn_tests_for", "Rank candidate tests using semantic references, naming, project, and path evidence.", ["symbol"], "repoRoot", "symbol", "maxResults"),
        Tool("roslyn_batch", "Execute a validated bounded operation graph against one immutable index generation.", ["operations"], "repoRoot", "operations", "failureMode", "maxChars", "timeoutMs"),
        Tool("roslyn_changes", "Return bounded semantic changes between generations or structural Git changes.", [], "repoRoot", "comparison", "baseId", "targetId", "maxResults"),
        Tool("roslyn_profile", "Return local generation, session, timing, table-size, and posting diagnostics.", [], "repoRoot", "topTerms"),
        Tool("roslyn_suggest", "Create deterministic index-backed query suggestions from a natural-language question.", ["question"], "repoRoot", "question", "limit", "executeTop"),
        Tool("roslyn_capabilities", "Return server, contract, repository binding, command, and limit capabilities.", [], "repoRoot")
    ];

    private static RoslynMcpToolDefinition Tool(string name, string description, string[] required, params string[] properties)
        => new(name, description, Schema(required, properties));

    private static RoslynMcpToolDefinition PReadTool()
    {
        var schema = BaseSchema(["filePath"], ["repoRoot", "filePath", "startLine", "endLine", "aroundLine", "context", "configPath", "maxTextFileBytes", "maxChars"]);
        schema["oneOf"] = new object[]
        {
            new Dictionary<string, object> { ["required"] = new[] { "startLine", "endLine" }, ["not"] = new Dictionary<string, object> { ["required"] = new[] { "aroundLine" } } },
            new Dictionary<string, object> { ["required"] = new[] { "aroundLine" }, ["not"] = new Dictionary<string, object> { ["anyOf"] = new object[] { new Dictionary<string, object> { ["required"] = new[] { "startLine" } }, new Dictionary<string, object> { ["required"] = new[] { "endLine" } } } } }
        };
        return new RoslynMcpToolDefinition("roslyn_pread", "Return a targeted partial read of a local repository file for focused inspection after full context is known.", JsonSerializer.Serialize(schema));
    }

    private static string Schema(string[] required, string[] properties)
        => JsonSerializer.Serialize(BaseSchema(required, properties));

    private static Dictionary<string, object> BaseSchema(string[] required, string[] properties)
        => new()
        {
            ["type"] = "object",
            ["properties"] = properties.ToDictionary(name => name, Property, StringComparer.Ordinal),
            ["required"] = required,
            ["additionalProperties"] = false
        };

    private static object Property(string name)
        => name switch
        {
            "force" or "includeGenerated" or "includeNonCSharpText" or "includeTests" or "includePrivate" or "includeExternal" or "exact" => Boolean(false),
            "mode" => Enum("all", "symbol", "text", "file", "reference"),
            "profile" => Enum("compact", "standard", "diagnostic"),
            "direction" => Enum("callers", "callees", "both"),
            "failureMode" => Enum("stop", "continue"),
            "comparison" => Enum("previousGeneration", "workingTreeHead", "generations", "commits"),
            "include" => new Dictionary<string, object> { ["type"] = "array", ["items"] = new Dictionary<string, object> { ["type"] = "string", ["enum"] = System.Enum.GetNames<InspectInclude>().Select(value => value[..1].ToLowerInvariant() + value[1..]).ToArray() }, ["uniqueItems"] = true },
            "operations" => new Dictionary<string, object>
            {
                ["type"] = "array",
                ["minItems"] = 1,
                ["maxItems"] = 20,
                ["items"] = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["id"] = new Dictionary<string, object> { ["type"] = "string", ["minLength"] = 1 },
                        ["operation"] = new Dictionary<string, object> { ["type"] = "string", ["enum"] = new[] { "goto", "refs", "outline", "inspect", "context", "callgraph", "impact", "tests_for" } },
                        ["query"] = new Dictionary<string, object> { ["type"] = "string" },
                        ["symbolFrom"] = new Dictionary<string, object> { ["type"] = "string" },
                        ["fileFrom"] = new Dictionary<string, object> { ["type"] = "string" },
                        ["depth"] = Integer(0, 8, 1),
                        ["limit"] = Integer(1, 1000, 50)
                    },
                    ["required"] = new[] { "id", "operation" },
                    ["additionalProperties"] = false
                }
            },
            "limit" => Integer(1, 1000, 50),
            "maxResults" => Integer(1, 1000, 80),
            "maxNodes" => Integer(1, 1000, 100),
            "topTerms" => Integer(1, 100, 20),
            "depth" => Integer(0, 8, 1),
            "executeTop" => Integer(0, 20, 0),
            "context" => Integer(0, 1000, 40),
            "startLine" or "endLine" or "aroundLine" => Integer(1, int.MaxValue),
            "timeoutMs" => Integer(0, 120_000),
            "timeoutSeconds" => Integer(1, 300),
            "maxDegreeOfParallelism" => Integer(1, 64),
            "maxTextFileBytes" => Integer(1, int.MaxValue),
            "maxChars" => Integer(1, 1_000_000, 30_000),
            _ => String(name)
        };

    private static Dictionary<string, object> String(string name)
        => new()
        {
            ["type"] = "string",
            ["minLength"] = name is "query" or "question" or "filePath" or "symbol" or "target" ? 1 : 0,
            ["maxLength"] = name is "repoRoot" or "filePath" or "configPath" ? 4096 : name == "continuationToken" ? 4096 : 1024
        };

    private static Dictionary<string, object> Boolean(bool defaultValue)
        => new() { ["type"] = "boolean", ["default"] = defaultValue };

    private static Dictionary<string, object> Enum(params string[] values)
        => new() { ["type"] = "string", ["enum"] = values, ["default"] = values[0] };

    private static Dictionary<string, object> Integer(int minimum, int maximum, int? defaultValue = null)
    {
        var schema = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = minimum, ["maximum"] = maximum };
        if (defaultValue is not null)
        {
            schema["default"] = defaultValue.Value;
        }

        return schema;
    }
}
