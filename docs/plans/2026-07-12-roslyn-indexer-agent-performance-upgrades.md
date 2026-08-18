# Plan: RoslynIndexer optimized for AI agents

**Date:** 2026-07-12
**Status:** Implemented
**Goal:** Transforming RoslynIndexer from a semantically correct CLI, but expensive at each call, into a persistent MCP service with index query-oriented, very low latency, compact output and compound commands built specifically for the workflow of an AI agent.

> The final implementation uses content-addressed binary segments per document instead of the initially sketched monolithic binary tables. The choice keeps the publish atomic, reduces the index Cerneala to about 13 MB and allows strict rewriting of dirty documents. Memory mapping was intentionally left unimplemented: buffered reads respect the initial load budget, so the additional complexity is not justified by the benchmark.

## Executive summary

RoslynIndexer already has the useful foundation: Roslyn semantic indexing, symbol searching, references, file reading, basic incrementality and MCP integration. The main blockage is not the lack of capabilities, but the query execution architecture.

In its current form, each call:

1. read the entire JSONL index from the disk;
2. deserializes all documents, symbols, references and postings;
3. rebuilds the lookup dictionaries in memory;
4. execute the query;
5. discard all status at the end of the call.

On the Cerneala repository, the measurements from 2026-07-12 were:

| Operation | Measured result |
|---|---:|
| Indexed C# Documents | 910 |
| Symbols | 24,861 |
| References | 53,048 |
| Token postings | 452,324 |
| Cold index C#-only | 17.7 s |
| No-op incremental, 0 dirty files | 15.1 s |
| Search one-shot | 2.1-2.6 s |
| Total index size | approximately 129 MB |
| `tokens.jsonl` | 88.9 MB |

The absolute priority is to reduce latency and repetitive work. Functionalities such as more sophisticated ranking or the `suggest` extension must not overtake persistent query state, real incrementality and lookup-oriented storage.

## Product principles

1. **The AI agent is the main client.** Contracts, orders and output are optimized for a small number of round-trips, low latency and low token consumption.
2. **MCP is the main surface.** The CLI remains useful for diagnostics, scripting and fallback, but does not dictate the MCP query path architecture.
3. **Cost proportional to the query.** An exact symbol lookup does not have to read 129 MB and reconstruct all the postings.
4. **No-op means no-op.** If the repository and configuration have not changed, indexing does not open MSBuildWorkspace and does not rewrite the index.
5. **Default compact output.** Duplicate fields, nulls with no value and redundant explanatory text are removed from MCP responses.
6. **Structured data, not prose.** Graphs, relationships and diagnostics are returned as nodes, edges, identifiers and stable codes.
7. **Budgets are part of the contract.** Compound orders accept limits for results, characters, nodes, depth and time.
8. **Semantic correctness is not sacrificed.** Optimization does not turn RoslynIndexer into a grep with a Roslyn hat on top.

## Measurable objectives

### Query path MCP

- The first query after starting MCP: under 500 ms on the Cerneala repository.
- Subsequent queries `search`: p50 under 20 ms, p95 under 50 ms.
- `goto` with exact ID or FQN symbol: p95 under 10 ms.
- `refs` indexed: p95 under 20 ms for a maximum of 100 results.
- No complete deserialization of the postings after the initial upload.
- No reconstruction of dictionaries between two calls on the same index generation.

### Indexing

- Incremental no-op: p95 under 100 ms, without starting MSBuild/Roslyn workspace.
- Body-only change in a file: under 1 s on Cerneala.
- Local declaration change: under 2 s when the project graph remains stable.
- Full cold index: initial target below 12 s on Cerneala, then optimization based on the profiler.
- Incremental persistence only rewrites the affected segments.

### Storage and memory

- Reduction of the Cerneala index from approximately 129 MB to a maximum of 50 MB in the first binary version.
- Elimination of string repetition for path, project, symbol ID and token.
- Stable MCP memory after warm-up, without proportional increase with the number of queries.
- Zero unnecessary integral copies of large collections on the query path.

### Output for agent

- The answers do not duplicate the same data in `data` and `results`.
- Every potentially large list supports `truncated` and `continuationToken`.
- Each command supports a profile `compact`, and this is default in MCP.
- Compound commands respect `maxResults`, `maxChars`, `maxNodes`, `depth` and timeout.

## Non-initial objectives

- AI, embeddings, vector database or cloud services.
- Network daemon or HTTP server.
- Complete semantic index for non-C# languages.
- Simultaneous restoration of all CLI commands.
- Ranking based on statistical models.
- Complex watcher before the deterministic fast path is proven.
- Eternal compatibility with old indexes; automatic rebuild is acceptable for major scheme change.

## Target architecture

### 1. `RepositoryIndexSession`

MCP keeps one session per repository:
```text
RepositorySessionRegistry
  repoRoot -> RepositoryIndexSession
                generationId
                manifestFingerprint
                QueryIndex
                async reload gate
                usage/latency counters
```
`RepositoryIndexSession` is responsible for:

- solving and normalizing a single `repoRoot`;
- charging the current generation;
- keeping lookups immutable;
- cheap verification of the generation before the query;
- reload only once when the manifest changes;
- atomic swap between generations;
- releasing the resources of the old generation after the end of the active readers.

No static global cache is introduced in Core. Session lifetime is explicitly owned by the MCP server and can be tested in isolation.

### 2. `QueryIndex`

`QueryIndex` contains only the structures necessary for queries:
```text
symbolId -> SymbolRecord
lowerName -> symbol IDs
lowerFqn -> symbol IDs
termId -> posting slice
symbolId -> reference slice
documentId -> DocumentRecord
pathId -> document ID
project graph adjacency
call graph adjacency
type hierarchy adjacency
```
Collections are immutable after publication. Queries can run concurrently without global lock.

### 3. Segmented binary storage

Proposed format:
```text
.roslyn-index/
  current.json
  generations/
    <generation-id>/
      manifest.json
      strings.bin
      documents.bin
      symbols.bin
      references.bin
      terms.bin
      postings.bin
      callgraph.bin
      hierarchy.bin
      diagnostics.jsonl
```
Rules:

- `current.json` atomically indicates the active generation.
- Tables use compact numeric IDs.
- Common strings are interned only once.
- Posting lists are ordered and delta-encoded.
- Numerical values ​​use variant where it brings measurable gain.
- Large tables can be memory-mapped.
- Each file has a header with magic, schema version, generation ID, row count and checksum.
- The writing is done in a temporary generation, validated, then published atomically.
- The previous generation remains available until the current readers release it.

No SQLite, Lucene or other external engine is added. The format remains local and controlled by the project.

### 4. Incrementality by segments

Indexing is explicitly divided into:

1. fingerprint repository/config;
2. workspace graph fingerprint;
3. file change detection;
4. syntax/declaration change classification;
5. semantic reindex plan;
6. segment goes;
7. atomic publish.

Fast path no-op stops after step 3. It does not open the workspace and does not rewrite the manifest just to prove that it had nothing to do.

For modified files:

- body-only: update tokens, call edges and affected local references;
- declaration change: invalidates the symbol and the necessary dependent projects;
- project/config change: rebuilds the workspace graph;
- scheme/tool ​​version change: full rebuild explicitly.

### 5. Agent response layer

The core returns rich semantic models. MCP applies a response profile:

- `compact`: strictly necessary fields, default;
- `standard`: includes snippets and matching explanations;
- `diagnostic`: includes timings, cache state and scoring details.

Truncation is done deterministically and is reported explicitly. No order accidentally returns hundreds of thousands of tokens.

## Common MCP contract

All new and existing orders must use a unique envelope:
```json
{
  "success": true,
  "tool": "roslyn_goto",
  "repoRoot": "C:/repo",
  "generationId": "01J...",
  "elapsedMs": 7,
  "cache": {
    "sessionHit": true,
    "generationReloaded": false
  },
  "truncated": false,
  "continuationToken": null,
  "data": {}
}
```
### Schema rules

- Exact JSON types, without generic union `string | number | boolean | null` for any property.
- Real enums for `mode`, `kind`, `direction`, `profile` and `include`.
- `minimum` and `maximum` for numerical limits.
- Correct required fields for each tool.
- Mutually exclusive rules for partial read variants.
- Defaults declared in the scheme.
- `additionalProperties: false`.
- Errors with `code`, `message`, `retryable` and `suggestedAction`.
- `repoRoot` becomes optional when the server is started repo-bound.
- The duplication between `data` and `results` is removed.

## New orders

### `roslyn_inspect`

The main command for understanding a symbol in a single round-trip.

Conceptual input:
```json
{
  "symbol": "UIElement.InvalidateMeasure",
  "include": [
    "source",
    "signature",
    "documentation",
    "containingType",
    "baseTypes",
    "members",
    "callers",
    "callees",
    "references",
    "implementations",
    "tests"
  ],
  "depth": 1,
  "maxResults": 80,
  "maxChars": 30000,
  "profile": "compact"
}
```
The output contains identifiers reusable by the other commands. The ambiguity is not hidden: if the query solves several symbols, the command returns the candidates and a code `ambiguous-symbol`.

### `roslyn_outline`

Returns the semantic structure of a file, type or namespace without the full content of the file.

It must include:

- namespaces and types;
- members with kind, accessibility, signature and span;
- base/interface relations;
- optional private/generated members;
- nesting until `depth`.

It is the default orientation command before `roslyn_read` for large files.

### `roslyn_context`

Build a compact package for a location or symbol:

- containing symbol;
- the relevant source fragment;
- declarations of direct dependencies;
- limited callers/callees;
- candidate tests;
- local diagnostics, if available.

The character budget is mandatory and respected after ordering the relevant.

### `roslyn_callgraph`

Returns structured graph:
```json
{
  "nodes": [
    { "id": "...", "name": "...", "kind": "method", "path": "...", "line": 10 }
  ],
  "edges": [
    { "from": "...", "to": "...", "kind": "invocation" }
  ]
}
```
It supports `direction: callers | callees | both`, `depth`, `maxNodes`, `includeTests` and `includeExternal`.

### `roslyn_impact`

It answers the question "what can be affected if I modify this symbol or file?".

Includes:

- callers and references;
- derived types and implementations;
- overrides;
- public API exposure;
- dependent projects;
- candidate tests;
- deterministic trust level and the reason for each link.

It does not claim to predict runtime behavior. Returns demonstrable semantic and structural impact.

### `roslyn_batch`

It performs several operations in a single round-trip and allows dependencies between them:
```json
{
  "operations": [
    { "id": "definition", "operation": "goto", "query": "UIElement" },
    { "id": "uses", "operation": "refs", "symbolFrom": "definition:0" },
    { "id": "shape", "operation": "outline", "fileFrom": "definition:0" }
  ],
  "maxChars": 40000,
  "timeoutMs": 1000
}
```
The permitted operations are listed explicitly; batch does not become a generic shell. A failure can be configured `stop` or `continue`, and each result keeps the operation ID.

### `roslyn_changes`

Produces semantic diff compared to:

- working tree versus `HEAD`;
- current versus previous generation index;
- two local commits;
- two generations of index.

Returns added, deleted and modified symbols, signature changes, public API changes and affected projects.

### `roslyn_tests_for`

Rank the relevant tests for symbol, file or change set using only:

- semantic references;
- call graph;
- project references;
- naming conventions;
- path proximity;
- optional local history only if available offline.

Each candidate includes scoring reasons. The command does not run the tests.
### `roslyn_capabilities`

Returns the server version, commands, index scheme, repository binding, session status and supported limits. This command solves the case where the agent does not know if the MCP is installed, configured or compatible.

### `roslyn_profile`

Local diagnosis for tool development, not for daily use. Returns:

- load/reload timings;
- query stage timings;
- allocation estimates available;
- the size of the segments;
- cache hit rates;
- top term posting sizes;
- no-op index breakdown.

It does not send telemetry and does not persist data outside of `.roslyn-index/`.

## Upgrades for existing orders

### `roslyn_search`

- Use direct lookup for exact symbol and exact FQN.
- Avoid scanning all symbols/references if there are exact candidates.
- Add `fields`, `profile`, `continuationToken` and explicit budgets.
- Returns `matchReason` as enum plus score components in diagnostic profile.
- Apply the timeout also in load/reload, not only in scoring.

### `roslyn_goto`

- Accept directly `symbolId` without textual query.
- Returns signature and declaration span compact.
- Differentiate declaration, partial declaration and generated declaration.

### `roslyn_refs`

- Optionally group by `referenceKind`, file or project.
- Supports stable paging.
- Exact refs uses an optionally reusable Roslyn session, does not start full workspace on every call.
- The exact cache is invalidated based on the symbol and the affected projects, not just by the global timestamp.

### `roslyn_read` and `roslyn_pread`

- Returns line numbers only when requested.
- Add `maxChars` and explicit truncation signal.
- Add `contentHash` to verify that the file has not changed between read and edit.
- `pread` accepts semantic span: containing member, declaration or body.

### `roslyn_status`

- It does not fully read JSONL files only for counts.
- Read counts and generation state directly from the manifest.
- Report separately `indexState`, `sessionState` and `workspaceState`.

### `roslyn_doctor`

- Includes MCP repo binding check.
- Includes scheme/tool ​​compatibility.
- Include the exact reason why the index is stale.
- It has a mode `quick` without opening the workspace and an explicit mode `deep`.

### `roslyn_suggest`

- It remains functional, but does not receive major investments before compound semantic commands.
- It can become a deterministic router to `inspect`, `impact`, `callgraph` and `tests_for`.
- It no longer generates CLI strings when the client is MCP; returns structured operations.

## Implementation stages

### Stage 0: reproducible baseline

- [ ] Add a deterministic benchmark corpus with small, medium and Cerneala-like classes.
- [ ] Separates the startup process time, index load, lookup build, scoring and snippet hydration.
- [ ] Measure p50/p95 after warm-up for at least 100 queries.
- [ ] Measure allocations and peak working set for load and query.
- [ ] Measures the size of each index file.
- [ ] Save the baseline as a test/benchmark artifact, not as a machine-dependent threshold in unit tests.
- [ ] Add a benchmark for 20 MCP calls in the same process.
- [ ] Add an incremental no-op benchmark that explicitly checks that MSBuild is not started.

**Gate:** No optimization is accepted without comparison with the baseline and without an equivalent functional test.

### Stage 1: persistent MCP session

- [ ] Enter `RepositorySessionRegistry` with lifetime owned by the MCP host.
- [ ] Enter `RepositoryIndexSession` and `QueryIndex` immutable.
- [ ] Load the index only once per ID generation.
- [ ] Detects the change of generation through a small reading of the current manifesto.
- [ ] Implement single-flight reload.
- [ ] Implements atomic swap and competition between readers.
- [ ] Add configurable eviction for several repositories, without aggressive timer.
- [ ] Instruments `sessionHit`, `reloadCount`, `loadMs` and `queryMs`.
- [ ] Add concurrent tests for query during reload.

**Gate:** 100 consecutive queries do not rebuild the lookups, and p95 warm search is under 50 ms on the Cerneala-like corpus.

### Stage 2: fast path for incremental no-op

- [ ] Separate discovery fingerprint from workspace load.
- [ ] Calculates config and workspace input fingerprint without Roslyn.
- [ ] Uses the available Git data for the list of changed files.
- [ ] Add deterministic filesystem fallback when Git is not available.
- [ ] Do not recalculate content hash for all files if metadata and Git confirm that they are unchanged.
- [ ] Returns immediately when the change set is empty.
- [ ] Do not rewrite the index, diagnostics or pointer generation on no-op.
- [ ] Add test that prohibits creating MSBuildWorkspace on no-op.

**Gate:** No-op incremental under 100ms p95 on Cerneala and zero files changed in `.roslyn-index/`.

### Stage 3: compact and strict MCP contract

- [ ] Replaces the generic schema with JSON Schema per tool.
- [ ] Eliminate duplicate field `results` or `data`.
- [ ] Add response profiles.
- [ ] Add structured error codes.
- [ ] Add paging and continuation tokens signed locally with generation ID.
- [ ] Fa `repoRoot` optional for repo-bound server.
- [ ] Add `roslyn_capabilities`.
- [ ] Tests exact serialization and schema-contract compatibility.

**Gate:** The compact answer for an exact `goto` is under 2 KB and contains no duplicate fields.

### Stage 4: query-oriented binary storage
- [ ] Defines the format and documents the invariants/headers.
- [ ] Enter string tables and numeric IDs.
- [ ] Implements writer and reader for documents/symbols.
- [ ] Implement term dictionary and posting slices.
- [ ] Implement references grouped by symbol ID.
- [ ] Add checksum and truncation/corruption detection.
- [ ] Add memory mapping only after benchmark compared to buffered reads.
- [ ] Keeps the JSONL reader temporarily for migration and comparative tests.
- [ ] Add the internal rebuild/migrate command via `roslyn_index --force`.
- [ ] Removes the old reader after an explicit period of compatibility.

**Gate:** Index under 50 MB on Cerneala, semantically equivalent byte-for-byte results and first load under 500 ms.

### Stage 5: incremental persistence on segments

- [ ] Enter per-document ownership for symbols, references and postings.
- [ ] Writes only the segments of dirty documents.
- [ ] Implements deterministic segment merge/compaction.
- [ ] Classify body-only versus declaration change with robust syntax/semantic declaration hash.
- [ ] Invalidates dependent projects only when the declaration form requires it.
- [ ] Keep the old generation until the publication and validation of the new one.
- [ ] Add recovery after process kill in each publishing stage.

**Gate:** Body-only modification of a file does not rewrite unrelated segments and finishes under 1 s on Cerneala.

### Stage 6: `roslyn_outline`, `roslyn_inspect` and `roslyn_context`

- [ ] Defines common models for symbol summary, source span and related item.
- [ ] Implements the outline from the index, without the Roslyn on-demand workspace.
- [ ] Implements strict resolver for ambiguous ID/FQN/query symbol.
- [ ] Implements inspect with include flags and budgets.
- [ ] Implements deterministic ranking context.
- [ ] Avoid duplicating the same fragment or symbol in the same answer.
- [ ] Add truncation and order stability tests.

**Gate:** Investigating a typical symbol requires a single call `inspect`, and the output follows `maxChars` exactly.

### Stage 7: call graph, hierarchy and impact

- [ ] Indexes invocation edges separately from generic references.
- [ ] Indexes base type, interface implementation and override edges.
- [ ] Enter `roslyn_callgraph` with bounded crossing.
- [ ] Enter `roslyn_impact` with deterministic reasons.
- [ ] Detects and marks external or unresolved nodes.
- [ ] Protects traversal against cycles and graph explosion.
- [ ] Add tests for overloads, extension methods, virtual dispatch and partial methods.

**Gate:** Graphs are stable, bounded and do not confuse overloads with the same name.

### Stage 8: batch, changes and test selection
- [ ] Implements the bounded executor for `roslyn_batch`.
- [ ] Validates references between operations before execution.
- [ ] Reuses the same session and the same generation for the entire batch.
- [ ] Implements semantic diff between generations.
- [ ] Integrates working tree/HEAD without network access.
- [ ] Implements `roslyn_tests_for` with explainable scoring.
- [ ] Add global limits for time, output and number of operations.

**Gate:** A `goto -> refs -> outline` batch makes a single generation check and has lower latency than three separate calls.

### Stage 9: hardening and local observability

- [ ] Enter `roslyn_profile` and consistent stage timings.
- [ ] Add stress test with concurrent queries and repeated reindexing.
- [ ] Add memory stability test on at least 10,000 queries.
- [ ] Add crash recovery test for each publishing point.
- [ ] Add incompatible scheme test and clear rebuild action.
- [ ] Add fuzz tests for query/schema/continuation token.
- [ ] Check path traversal and strict isolation at the root repo.

**Gate:** No corruption, deadlock, continuous memory growth or response from a mixed generation.

## Test strategy

### Unit tests

- binary and variable codec;
- string tables;
- posting list encode/decode;
- pointer generation;
- query exact/prefix/token;
- paging and continuation token;
- bounded traversal graph;
- response profile and truncation;
- config/fingerprint classification.

### Integration tests

- full index -> load -> query;
- incremental no-op;
- body-only update;
- declaration update;
- project reference update;
- concurrent query with atomic generation swap;
- process kill before and after publish;
- corrupt/truncated index;
- repo-bound server versus explicit root repo;
- backward compatibility during migration.

### Benchmarks

- BenchmarkDotNet for codec, load and query hot paths;
- separate end-to-end MCP test with persistent process;
- small/medium/large versioned corpora;
- p50/p95 results and allocations;
- budget size per table;
- comparison with the baseline from Stage 0.

Functional tests do not use wide thresholds of tens of seconds as proof of performance. Hardware-sensitive performance budgets run in a dedicated benchmark job; regular CI tests check invariants such as "workspace loader was not called" and "index was not rewritten".

## Migration and compatibility

1. Increase the index scheme and add `storageFormat` to the manifest.
2. The Reader detects old JSONL and responds with the structured action `rebuild-required`.
3. `roslyn_index` rebuilds in the new format in a separate generation.
4. MCP continues to serve the old generation until the new one is validated.
5. After publish, the session does atomic swap.
6. Do not attempt to convert old files in-place.

Existing MCP contracts remain available initially, but new compact responses must be versioned clearly if deduplication `data/results` is breaking.

## Risks and measures

### Too much memory in the MCP process

Persistent session can move the cost from CPU to memory. The working set is measured, compact structures and memory mapping are used only where the benchmark demonstrates a gain. The registry has explicit eviction for inactive repositories.

### Hard-to-maintain binary format

The format receives specification, versioned headers, small readers per table, golden fixtures and corruption tests. A general mini-database is not invented; the necessary operations are strictly implemented.

### Semantically incorrect incrementality

Body-only and declaration-change are validated through differential tests: the incremental result must be identical to a full rebuild on the same repository.

### Reload during the query

Each query captures an immutable generation. Do not combine data from different generations. The old generation is released only after the release of the last reader.

### Compound commands with explosive output

All traversals have depth/maxNodes/maxResults/maxChars and timeout. Truncation is deterministic, visible and pageable.

### Premature optimization of the cold index

Cold index is important, but query latency and incremental no-op have higher priority. It is profiled before parallelization or complicated semantic caching.

## Recommended delivery order

1. Baseline and instrumentation.
2. Persistent MCP session.
3. Fast path no-op.
4. Contract MCP compact and `roslyn_capabilities`.
5. Binary storage.
6. Incremental persistence on segments.
7. `outline`, `inspect`, `context`.
8. Call graph and impact.
9. Batch, semantic changes and test selection.
10. Hardening, profiling and elimination of the old format.

It doesn't start with `suggest` or sophisticated ranking. Making smarter suggestions over a 2.5 second query means putting a spoiler on the tractor.

## Final check

1. Run the complete build without warnings and errors.
2. Run all RoslynRepoIndexer tests.
3. Run the full versus incremental differential tests.
4. Run the benchmarks on the established corpora.
5. Check the p50/p95 budgets, allocations, working set and disk size.
6. Run at least 10,000 queries in a single MCP session.
7. Run concurrent queries during reindexing.
8. Simulate process kill and validate the recovery of the previous generation.
9. Check all JSON MCP Schemas against C# contracts.
10. Run `git diff --check`.
11. Regenerate `FileTree.md` if new files appear.
12. Reindex `Cerneala.slnx` and confirm valid index, without dirty files or warnings.

## Acceptance criteria

The implementation is considered complete only when:

- MCP queries reuse a persistent session and an immutable generation;
- warm search p95 is below 50 ms on the Cerneala-like corpus;
- incremental no-op is below 100 ms and does not open MSBuildWorkspace;
- the index Cerneala occupies a maximum of 50 MB;
- a body-only change does not cause full persist or full semantic rebuild;
- MCP contracts are strict, compact and without duplicate fields;
- `inspect`, `outline`, `context`, `callgraph`, `impact`, `batch`, `changes` and `tests_for` are bounded and tested;
- incremental build produces the same results as full rebuild;
- concurrent queries do not observe partial generations;
- crash recovery keeps the last valid generation;
- the memory remains stable for 10,000 queries;
- the tool's documentation describes the architecture, commands and real performance budgets.

## Decisions that must be confirmed before implementation

1. Custom binary format versus keeping JSONL for small tables; the recommendation is binary for query tables and JSON only for manifest/diagnostics.
2. Memory mapping from the first version versus after the buffered binary reader; the recommendation is benchmark first, then choice.
3. Breaking change immediately for the elimination of `data/results` versus temporary versioning; the recommendation is a new version of the MCP contract.
4. A single repo-bound session versus multi-repo registry; the recommendation is internal multi-repo support, but default repo-bound configuration.
5. Exact Roslyn persistent workspace versus on-demand cache; the recommendation is to postpone it until after optimizing the indexed refs and measuring the real usage.
