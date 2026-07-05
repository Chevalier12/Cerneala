namespace Ri.Mcp;

public static class RoslynMcpToolCatalog
{
    public static IReadOnlyList<RoslynMcpToolDefinition> Tools { get; } =
    [
        Tool("roslyn_doctor", "Inspect local repository/index prerequisites and diagnostics.", "repoRoot", "configPath"),
        Tool("roslyn_index", "Build or update the local Roslyn repository index. Writes only under .roslyn-index in the repo root.", "repoRoot", "force", "includeGenerated", "includeNonCSharpText", "maxTextFileBytes", "maxDegreeOfParallelism", "configPath"),
        Tool("roslyn_status", "Return deterministic local index status for the repository.", "repoRoot"),
        Tool("roslyn_search", "Search the existing local index for symbols, text, files, or references.", "repoRoot", "query", "mode", "limit", "kind", "path", "project", "includeTests", "fromFile", "fromProject", "timeoutMs"),
        Tool("roslyn_read", "Return a full local repository file. Prefer roslyn_read before editing so the complete file context is available.", "repoRoot", "filePath", "configPath", "maxTextFileBytes"),
        Tool("roslyn_pread", "Return a targeted partial read of a local repository file for focused inspection after full context is known.", "repoRoot", "filePath", "startLine", "endLine", "aroundLine", "context", "configPath", "maxTextFileBytes"),
        Tool("roslyn_goto", "Find symbol declarations in the local index.", "repoRoot", "query", "kind", "limit"),
        Tool("roslyn_refs", "Find indexed or exact Roslyn references for a symbol without shell execution or network calls.", "repoRoot", "query", "symbolId", "exact", "timeoutSeconds", "limit"),
        Tool("roslyn_suggest", "Create deterministic index-backed query suggestions from a natural-language question.", "repoRoot", "question", "limit", "executeTop")
    ];

    private static RoslynMcpToolDefinition Tool(string name, string description, params string[] properties)
        => new(name, description, Schema(properties));

    private static string Schema(IReadOnlyList<string> properties)
    {
        var props = string.Join(",", properties.Select(property => $"\"{property}\":{{\"description\":\"{property}\",\"type\":[\"string\",\"number\",\"boolean\",\"null\"]}}"));
        return "{\"type\":\"object\",\"properties\":{" + props + "},\"required\":[\"repoRoot\"],\"additionalProperties\":false}";
    }
}
