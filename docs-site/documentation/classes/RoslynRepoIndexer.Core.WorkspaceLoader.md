# WorkspaceLoader Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynIndexing.cs`

Represents the public WorkspaceLoader contract used by Roslyn Repo Indexer.

```csharp
public sealed class WorkspaceLoader
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `WorkspaceLoader()` | Initializes a new instance. |

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `Discover(string repoRoot, IndexerConfig config)` | `IReadOnlyList<WorkspaceInput>` | Executes the `Discover` operation. |
| `LoadAsync(string repoRoot, WorkspaceInput input, CancellationToken cancellationToken)` | `Task<LoadedWorkspace>` | Executes the `LoadAsync` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
