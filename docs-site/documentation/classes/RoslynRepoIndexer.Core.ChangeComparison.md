# ChangeComparison Enum

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SemanticChanges.cs`

Specifies the supported values for ChangeComparison.

```csharp
public enum ChangeComparison
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `PreviousGeneration` | 0 | Selects the `PreviousGeneration` behavior. |
| `WorkingTreeHead` | 1 | Selects the `WorkingTreeHead` behavior. |
| `Generations` | 2 | Selects the `Generations` behavior. |
| `Commits` | 3 | Selects the `Commits` behavior. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
