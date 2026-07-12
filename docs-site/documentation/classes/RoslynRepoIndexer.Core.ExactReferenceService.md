# ExactReferenceService Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynIndexing.cs`

Provides deterministic local operations for ExactReferenceService scenarios.

```csharp
public sealed class ExactReferenceService
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `ExactReferenceService()` | Initializes a new instance. |

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `FindExactAsync(string repoRoot, string symbolIdOrQuery, int timeoutSeconds, CancellationToken cancellationToken)` | `Task<IReadOnlyList<SearchResult>>` | Executes the `FindExactAsync` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
