# IndexStatus Enum

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Specifies the supported values for IndexStatus.

```csharp
public enum IndexStatus
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Missing` | 0 | Selects the `Missing` behavior. |
| `Valid` | 1 | Selects the `Valid` behavior. |
| `Stale` | 2 | Selects the `Stale` behavior. |
| `Corrupt` | 3 | Selects the `Corrupt` behavior. |
| `SchemaIncompatible` | 4 | Selects the `SchemaIncompatible` behavior. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
