# Roslyn Repo Indexer

`ri` is a local .NET tool that builds a repository search index with Roslyn semantic data for C# and a small custom inverted text index for other text files. It writes only under `.roslyn-index/` in the repository root. It does not use AI, embeddings, vector databases, HTTP calls, telemetry, cloud APIs, Lucene, ElasticSearch, SQLite FTS, or any external search service.

## Build and Test

```bash
dotnet build tools/RoslynRepoIndexer/RoslynRepoIndexer.sln
dotnet test tools/RoslynRepoIndexer/RoslynRepoIndexer.sln
```

## Usage

```bash
dotnet run --project tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- index .
dotnet run --project tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- status .
dotnet run --project tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- doctor .
dotnet run --project tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- search CustomerService
dotnet run --project tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- suggest "unde se validează tokenul JWT?"
dotnet run --project tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- refs CustomerService --exact
```

The CLI can also be packed or installed as a .NET tool because `RoslynRepoIndexer.Cli` sets `PackAsTool=true` and `ToolCommandName=ri`.

## MCP Server for Codex

`Ri.Mcp` exposes the same local Roslyn indexer services over MCP using stdio transport. It is repo-bound and only exposes read, search, status, doctor, index, goto, refs, and suggest tools. It does not expose shell execution or file-write tools.

For Codex/MCP indexing, `includeNonCSharpText` defaults to `false` when omitted. Pass `includeNonCSharpText: true` explicitly only when non-C# text indexing is needed; it can dominate index time on large repositories.

Build it:

```bash
dotnet build tools/RoslynRepoIndexer/src/Ri.Mcp/Ri.Mcp.csproj
```

Run metadata commands:

```bash
dotnet run --project tools/RoslynRepoIndexer/src/Ri.Mcp -- --help
dotnet run --project tools/RoslynRepoIndexer/src/Ri.Mcp -- --version
```

Example Codex MCP config:

```toml
[mcp_servers.roslyn-indexer]
command = "dotnet"
args = ["run", "--project", "tools/RoslynRepoIndexer/src/Ri.Mcp", "--"]
cwd = "C:/path/to/your/repo"
```

Available tools are registered in deterministic order:

```text
roslyn_doctor
roslyn_index
roslyn_status
roslyn_search
roslyn_read
roslyn_pread
roslyn_goto
roslyn_refs
roslyn_suggest
```

Prefer `roslyn_read` before editing so the model sees the full file. Use `roslyn_pread` only for targeted partial reads after the relevant file and range are already known.

## JSON Examples

Search:

```json
{
  "success": true,
  "exitCode": 0,
  "command": "search",
  "query": "CustomerService",
  "repoRoot": "C:/repo",
  "elapsedMs": 12,
  "indexUpdatedUtc": "2026-07-04T00:00:00+00:00",
  "data": [
    {
      "path": "src/CustomerService.cs",
      "filePath": "src/CustomerService.cs",
      "line": 3,
      "column": 21,
      "startLine": 3,
      "startColumn": 21,
      "endLine": 3,
      "endColumn": 42,
      "kind": "class",
      "score": 800,
      "matchReason": "symbol match: My.App.CustomerService",
      "symbolId": "abc123",
      "symbolName": "CustomerService",
      "fullyQualifiedName": "My.App.CustomerService",
      "projectName": "My.App",
      "snippet": "public sealed class CustomerService"
    }
  ],
  "results": [
    {
      "path": "src/CustomerService.cs",
      "filePath": "src/CustomerService.cs",
      "line": 3,
      "column": 21,
      "startLine": 3,
      "startColumn": 21,
      "endLine": 3,
      "endColumn": 42,
      "kind": "class",
      "score": 800,
      "matchReason": "symbol match: My.App.CustomerService",
      "symbolId": "abc123",
      "symbolName": "CustomerService",
      "fullyQualifiedName": "My.App.CustomerService",
      "projectName": "My.App",
      "snippet": "public sealed class CustomerService"
    }
  ],
  "warnings": [],
  "errors": []
}
```

Suggest:

```json
{
  "success": true,
  "exitCode": 0,
  "command": "suggest",
  "query": "unde se validează tokenul JWT?",
  "repoRoot": "C:/repo",
  "elapsedMs": 4,
  "data": [
    {
      "command": "ri search \"jwt token validate validation validator\"",
      "query": "jwt token validate validation validator",
      "mode": "all",
      "confidence": 0.7,
      "reason": "deterministic token search",
      "expectedResultKind": "mixed"
    }
  ],
  "results": [
    {
      "command": "ri search \"jwt token validate validation validator\"",
      "query": "jwt token validate validation validator",
      "mode": "all",
      "confidence": 0.7,
      "reason": "deterministic token search",
      "expectedResultKind": "mixed"
    }
  ],
  "warnings": [],
  "errors": []
}
```

Status:

```json
{
  "success": true,
  "exitCode": 0,
  "command": "status",
  "repoRoot": "C:/repo",
  "elapsedMs": 2,
  "data": {
    "status": "valid",
    "indexState": "valid",
    "schemaVersion": 3,
    "documents": 42,
    "symbols": 120,
    "references": 300,
    "tokens": 5000,
    "dirtyFiles": 0,
    "warnings": []
  },
  "warnings": [],
  "errors": []
}
```

Doctor:

```json
{
  "success": true,
  "exitCode": 0,
  "command": "doctor",
  "repoRoot": "C:/repo",
  "elapsedMs": 25,
  "data": {
    "repoRoot": "C:/repo",
    "checks": [
      {
        "name": "repo-root",
        "status": "pass",
        "severity": "info",
        "message": "C:/repo",
        "details": {}
      }
    ]
  },
  "warnings": [],
  "errors": []
}
```

## Configuration

If `.roslyn-index.json` exists in the repo root, `ri index` reads it automatically.

```json
{
  "solution": null,
  "includeGenerated": false,
  "includeNonCSharpText": true,
  "maxTextFileBytes": 1048576,
  "maxDegreeOfParallelism": 4,
  "searchResultLimit": 50,
  "suggestionLimit": 5,
  "exactRefsTimeoutSeconds": 30,
  "excludeDirectories": [".git", "bin", "obj", ".vs", ".idea", ".vscode", "node_modules", ".roslyn-index", "TestResults", "artifacts", "packages"],
  "excludeFileSuffixes": [".dll", ".exe", ".pdb", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".ico", ".pdf", ".zip", ".7z", ".tar", ".gz"]
}
```

There is no `.riignore`; exclusions live in `.roslyn-index.json` so the config stays explicit and serializable.

## Performance Budgets

Repository size classes used for smoke testing and planning:

- `small`: fewer than 500 files.
- `medium`: 500 to 5,000 files.
- `large`: 5,000 to 25,000 files.

Daily-use budgets are intentionally relaxed so normal tests do not become flaky:

- Cold index: complete without out-of-memory on small and medium repositories.
- Warm incremental index with no changes: report `dirtyDocuments = 0` and avoid semantic reprocessing.
- Warm incremental index after one file change: reprocess only the changed document when declaration shape is unchanged.
- `ri search`: target under 1 second on small/medium repositories after the index is warm.
- `ri goto`: target under 1 second on small/medium repositories after the index is warm.
- `ri suggest`: target under 1 second on small/medium repositories after the index is warm.
- Approximate `ri refs`: target under 1 second because it uses indexed references and does not start MSBuild/Roslyn.
- Exact `ri refs --exact`: use Roslyn on demand and respect `exactRefsTimeoutSeconds` or `--timeout`.
- Memory: keep indexing bounded by processing projects/documents sequentially or with limited configured parallelism; smoke tests should catch out-of-memory behavior rather than enforce a fragile byte threshold.

## Exit Codes

- `0`: success
- `1`: user/input error
- `2`: invalid command or arguments
- `3`: index missing, corrupt, unavailable, or schema-incompatible
- `4`: unexpected internal error
- `5`: timeout or cancellation

## Troubleshooting

- Run `ri doctor . --json` to inspect repo root detection, workspace inputs, MSBuild registration, workspace loading, excludes, and index status.
- If search says the index is missing, run `ri index .`.
- If MSBuild cannot be detected, ensure `dotnet --list-sdks` returns an SDK folder containing `MSBuild.dll`.
- Generated files are excluded by default; pass `--include-generated` to index them.
