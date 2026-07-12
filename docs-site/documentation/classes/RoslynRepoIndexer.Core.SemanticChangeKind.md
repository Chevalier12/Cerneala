# SemanticChangeKind Enum

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SemanticChanges.cs`

Specifies the supported values for SemanticChangeKind.

```csharp
public enum SemanticChangeKind
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Added` | 0 | Selects the `Added` behavior. |
| `Removed` | 1 | Selects the `Removed` behavior. |
| `Modified` | 2 | Selects the `Modified` behavior. |
| `Touched` | 3 | Selects the `Touched` behavior. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
