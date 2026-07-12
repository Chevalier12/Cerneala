# IndexBuilder Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynIndexing.cs`

Represents the public IndexBuilder contract used by Roslyn Repo Indexer.

```csharp
public sealed class IndexBuilder
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `IndexBuilder(RepositoryWorkspaceSession workspaceSession)` | Initializes a new instance. |

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `BuildAsync(string startPath, bool force, IndexerConfig config, CancellationToken cancellationToken)` | `Task<IndexSummary>` | Executes the `BuildAsync` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
