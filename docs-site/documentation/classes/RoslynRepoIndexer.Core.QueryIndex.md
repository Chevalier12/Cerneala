# QueryIndex Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RepositorySessions.cs`

Represents the public QueryIndex contract used by Roslyn Repo Indexer.

```csharp
public sealed class QueryIndex
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `QueryIndex(IndexSnapshot snapshot)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Snapshot` | `IndexSnapshot` | Gets the Snapshot value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
