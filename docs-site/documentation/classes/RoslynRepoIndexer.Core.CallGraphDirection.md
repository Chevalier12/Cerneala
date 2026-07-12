# CallGraphDirection Enum

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SemanticQueries.cs`

Specifies the supported values for CallGraphDirection.

```csharp
public enum CallGraphDirection
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Callers` | 0 | Selects the `Callers` behavior. |
| `Callees` | 1 | Selects the `Callees` behavior. |
| `Both` | 2 | Selects the `Both` behavior. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
