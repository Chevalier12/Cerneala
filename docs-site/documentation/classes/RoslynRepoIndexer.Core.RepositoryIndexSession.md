# RepositoryIndexSession Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RepositorySessions.cs`

Represents the public RepositoryIndexSession contract used by Roslyn Repo Indexer.

```csharp
public sealed class RepositoryIndexSession
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RepositoryIndexSession(string repoRoot)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Metrics` | `RepositorySessionMetrics` | Gets the Metrics value. |
| `SessionState` | `string` | Gets the SessionState value. |
| `WorkspaceState` | `string` | Gets the WorkspaceState value. |

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `CreateIndexBuilder()` | `IndexBuilder` | Executes the `CreateIndexBuilder` operation. |
| `Dispose()` | `void` | Executes the `Dispose` operation. |
| `GetQueryIndex()` | `QueryIndex` | Executes the `GetQueryIndex` operation. |
| `GetQueryIndexAsync(CancellationToken cancellationToken)` | `ValueTask<QueryIndex>` | Executes the `GetQueryIndexAsync` operation. |
| `RecordQuery(long elapsedMs)` | `void` | Executes the `RecordQuery` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
