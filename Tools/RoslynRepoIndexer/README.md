# Roslyn Repo Indexer

`ri` is a local Roslyn indexer optimized for AI agents. The MCP server keeps an immutable query index in memory per repository, while the CLI remains available for diagnostics and scripting. The tool uses no network service, telemetry, embeddings, database, or shell execution through MCP. Persistent files are confined to `.roslyn-index/`.

## Build and test

```powershell
dotnet build Tools/RoslynRepoIndexer/RoslynRepoIndexer.slnx -c Release
dotnet test Tools/RoslynRepoIndexer/RoslynRepoIndexer.slnx -c Release
```

## Architecture

- `RepositorySessionRegistry` owns a bounded LRU set of repository sessions.
- `RepositoryIndexSession` loads one generation, builds `QueryIndex` once, performs single-flight reloads, and atomically publishes the replacement.
- Every query captures one immutable generation. Concurrent queries never mix rows from two generations.
- `RepositoryWorkspaceSession` reuses an `MSBuildWorkspace` for incremental indexing and reloads it when project shape, globbed source files, or workspace inputs change.
- The no-op path checks repository/config/workspace fingerprints before opening Roslyn and never rewrites the generation pointer.

## Storage

Schema 6 uses content-addressed binary document segments:

```text
.roslyn-index/
  current.json
  state.json
  generations/<generation-id>/
    manifest.json
    segments.json
  segments/<sha256>.bin
```

Each segment owns its document rows, symbols, references, and token postings. Its envelope contains magic, schema version, lengths, raw checksum, payload checksum, and a content-addressed filename. Segment payloads use a local string table and compact typed rows. Incremental publish writes only dirty segments, validates the new generation, atomically replaces `current.json`, and retains the previous valid generation for recovery. Corrupt or truncated current generations fall back to the previous valid generation.

Only `current.json` selects the active generation. Orphan temporary generations are never served. The storage reader validates paths, descriptor metadata, lengths, checksums, schema, and content hashes before returning rows.

## CLI

```powershell
dotnet run -c Release --project Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- index .
dotnet run -c Release --project Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- status .
dotnet run -c Release --project Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- doctor .
dotnet run -c Release --project Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- doctor . --deep
dotnet run -c Release --project Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- search UIElement
dotnet run -c Release --project Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- refs UIElement
```

`doctor` is quick by default and does not open `MSBuildWorkspace`; `--deep` explicitly validates workspace loading. The CLI can be packed as a .NET tool with command name `ri`.

## MCP server

`Ri.Mcp` uses stdio transport and can be bound to its startup repository:

```toml
[mcp_servers.roslyn-indexer]
command = "dotnet"
args = ["run", "-c", "Release", "--project", "Tools/RoslynRepoIndexer/src/Ri.Mcp", "--"]
cwd = "C:/path/to/repo"
```

`repoRoot` may be omitted for a repo-bound server. Explicit roots outside that binding are rejected. MCP indexing defaults to C# only unless `includeNonCSharpText: true` is supplied.

Available tools:

```text
roslyn_capabilities  roslyn_doctor   roslyn_index    roslyn_status
roslyn_search        roslyn_read     roslyn_pread    roslyn_goto
roslyn_refs          roslyn_outline  roslyn_inspect  roslyn_context
roslyn_callgraph     roslyn_impact   roslyn_tests_for
roslyn_batch         roslyn_changes  roslyn_profile  roslyn_suggest
```

The compound tools are index-only: they do not open Roslyn on the query path. `roslyn_refs` opens a workspace only when exact references are explicitly requested.

## MCP contract

Every response uses one compact envelope and does not duplicate `data` as `results`:

```json
{
  "success": true,
  "tool": "roslyn_goto",
  "repoRoot": "C:/repo",
  "generationId": "20260712...",
  "elapsedMs": 4,
  "cache": { "sessionHit": true, "generationReloaded": false },
  "truncated": false,
  "continuationToken": null,
  "data": []
}
```

Schemas are tool-specific, reject additional properties, use bounded numeric and string inputs, and expose exact enums. Errors contain stable `code`, `message`, `retryable`, and `suggestedAction` fields. Potentially large results expose deterministic truncation and signed continuation tokens bound to repository, tool, query, and generation. A token from another generation is rejected rather than returning a mixed page.

Profiles are `compact` (MCP default), `standard`, and `diagnostic`. Compound commands enforce their applicable `maxResults`, `maxChars`, `maxNodes`, `depth`, and timeout budgets. `roslyn_batch` validates all dependencies before execution and captures one generation for the complete operation graph.

## Command intent

- `outline`: structure of a file, namespace, or type without reading the complete file.
- `inspect`: strict symbol resolution plus selected source and relationships in one call.
- `context`: relevance-ranked source and dependency package within a character budget.
- `callgraph`: bounded invocation graph with cycle protection.
- `impact`: demonstrable references, callers, hierarchy, projects, and tests with reasons.
- `tests_for`: deterministic candidate ranking with explainable evidence; it never runs tests.
- `changes`: semantic generation diff or structural local Git diff without network access.
- `profile`: local session, load, query, segment-size, and posting diagnostics.
- `suggest`: deterministic structured MCP operations rather than CLI command strings.

## Configuration

If `.roslyn-index.json` exists at the repository root, `ri index` reads it automatically.

```json
{
  "solution": null,
  "includeGenerated": false,
  "includeNonCSharpText": true,
  "maxTextFileBytes": 1048576,
  "maxDegreeOfParallelism": 8,
  "searchResultLimit": 50,
  "suggestionLimit": 5,
  "exactRefsTimeoutSeconds": 30
}
```

Default exclusions cover `.git`, build outputs, IDE state, packages, `.roslyn-index`, test results, and common binary suffixes. There is no `.riignore`; configuration stays explicit and serializable.

## Benchmarks

The benchmark project contains deterministic small (10 files), medium (100 files), and Cerneala-like corpora. It separates cold storage load plus lookup construction, warm search, 20 persistent MCP calls, and no-op indexing.

```powershell
dotnet run -c Release --project Tools/RoslynRepoIndexer/benchmarks/RoslynRepoIndexer.Benchmarks -- --job short
```

Set `RI_BENCH_REPO` to benchmark another indexed repository. Results go under `Tools/RoslynRepoIndexer/benchmarks/artifacts/`; the checked-in baseline is `benchmarks/baseline-2026-07-12.json`.

Measured on Cerneala (910 C# documents, 24,861 symbols) on 2026-07-12:

| Budget or measurement | Result |
|---|---:|
| Binary index size | about 13 MB (budget: 50 MB) |
| First buffered load plus lookup build | about 420 ms (budget: 500 ms) |
| Warm exact search | about 0.06 ms |
| Warm broad search | about 4.4-6.6 ms |
| No-op incremental | about 78 ms (budget: 100 ms) |
| 20 persistent MCP calls | about 117-276 ms total |
| 10,000 queries | stable retained memory, under 4 MB growth in the stress gate |
| Full cold C# index | about 15-17 s; the initial 12 s target remains profiler work, not an acceptance gate |

Performance thresholds that depend on hardware live in benchmark jobs. Functional tests prove the important invariants: no workspace on no-op, unchanged segment reuse, differential full/incremental equivalence, stable generation capture, bounded output, and corruption recovery.

## Exit codes

- `0`: success
- `1`: invalid user input
- `2`: invalid command or arguments
- `3`: missing, corrupt, unavailable, or schema-incompatible index
- `4`: unexpected internal error
