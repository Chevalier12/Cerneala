# InspectInclude Enum

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SemanticQueries.cs`

Specifies the supported values for InspectInclude.

```csharp
public enum InspectInclude
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Source` | 0 | Selects the `Source` behavior. |
| `Signature` | 1 | Selects the `Signature` behavior. |
| `Documentation` | 2 | Selects the `Documentation` behavior. |
| `ContainingType` | 3 | Selects the `ContainingType` behavior. |
| `BaseTypes` | 4 | Selects the `BaseTypes` behavior. |
| `Members` | 5 | Selects the `Members` behavior. |
| `Callers` | 6 | Selects the `Callers` behavior. |
| `Callees` | 7 | Selects the `Callees` behavior. |
| `References` | 8 | Selects the `References` behavior. |
| `Implementations` | 9 | Selects the `Implementations` behavior. |
| `Tests` | 10 | Selects the `Tests` behavior. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
