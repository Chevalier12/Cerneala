# RepositorySessionMetrics Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RepositorySessions.cs`

Represents the public RepositorySessionMetrics contract used by Roslyn Repo Indexer.

```csharp
public sealed class RepositorySessionMetrics
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RepositorySessionMetrics(long SessionHits, long ReloadCount, long LoadMs, long QueryCount, long TotalQueryMs)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `LoadMs` | `long` | Gets or sets the LoadMs value. |
| `QueryCount` | `long` | Gets or sets the QueryCount value. |
| `ReloadCount` | `long` | Gets or sets the ReloadCount value. |
| `SessionHits` | `long` | Gets or sets the SessionHits value. |
| `TotalQueryMs` | `long` | Gets or sets the TotalQueryMs value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
