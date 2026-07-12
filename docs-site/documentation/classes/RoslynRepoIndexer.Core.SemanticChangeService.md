# SemanticChangeService Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SemanticChanges.cs`

Provides deterministic local operations for SemanticChangeService scenarios.

```csharp
public sealed class SemanticChangeService
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `SemanticChangeService(string repoRoot)` | Initializes a new instance. |

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `Compare(ChangeComparison comparison, string baseId, string targetId, int maxResults)` | `SemanticChangesResult` | Executes the `Compare` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
