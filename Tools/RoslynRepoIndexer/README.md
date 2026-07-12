# Roslyn Repo Indexer

`ri` is a local Roslyn indexer optimized for AI agents. The CLI uses repository-scoped background sessions to reuse immutable query indexes across invocations. The tool uses no network service, telemetry, embeddings, or database. Persistent files are confined to `.roslyn-index/`.

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

Query commands (`search`, `refs`, `goto`, `symbols`, and `status`) automatically reuse a repository-scoped background session for ten minutes, so separate CLI invocations do not reload the same index. The session runs from a shadow copy and does not lock build outputs. Set `RI_DISABLE_DAEMON=1` for isolated scripts and tests.

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
  "exactRefsTimeoutSeconds": 30
}
```

Default exclusions cover `.git`, build outputs, IDE state, packages, `.roslyn-index`, test results, and common binary suffixes. There is no `.riignore`; configuration stays explicit and serializable.

## Benchmarks

The benchmark project contains deterministic small (10 files), medium (100 files), and Cerneala-like corpora. It separates cold storage load plus lookup construction, warm search, 20 persistent application-service calls, and no-op indexing.

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
| 20 persistent application-service calls | about 117-276 ms total |
| Full cold C# index | about 15-17 s; the initial 12 s target remains profiler work, not an acceptance gate |

Performance thresholds that depend on hardware live in benchmark jobs. Functional tests prove the important invariants: no workspace on no-op, unchanged segment reuse, differential full/incremental equivalence, stable generation capture, bounded output, and corruption recovery.

## Exit codes

- `0`: success
- `1`: invalid user input
- `2`: invalid command or arguments
- `3`: missing, corrupt, unavailable, or schema-incompatible index
- `4`: unexpected internal error
