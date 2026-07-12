# SemanticQueryService Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SemanticQueries.cs`

Provides deterministic local operations for SemanticQueryService scenarios.

```csharp
public sealed class SemanticQueryService
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `SemanticQueryService(QueryIndex index, string repoRoot)` | Initializes a new instance. |

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `CallGraph(string query, CallGraphDirection direction, int depth, int maxNodes, bool includeTests, bool includeExternal)` | `CallGraphResult` | Executes the `CallGraph` operation. |
| `Context(string query, int maxChars, int maxResults)` | `ContextResult` | Executes the `Context` operation. |
| `Impact(string query, int maxResults)` | `ImpactResult` | Executes the `Impact` operation. |
| `Inspect(string query, IReadOnlyCollection<InspectInclude> include, int depth, int maxResults, int maxChars)` | `InspectResult` | Executes the `Inspect` operation. |
| `Outline(string target, int depth, int maxResults, int maxChars, bool includePrivate, bool includeGenerated)` | `OutlineResult` | Executes the `Outline` operation. |
| `TestsFor(string query, int maxResults)` | `IReadOnlyList<TestCandidate>` | Executes the `TestsFor` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
