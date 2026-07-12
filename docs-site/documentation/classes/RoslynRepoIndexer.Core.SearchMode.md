# SearchMode Enum

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Specifies the supported values for SearchMode.

```csharp
public enum SearchMode
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `All` | 0 | Selects the `All` behavior. |
| `Symbol` | 1 | Selects the `Symbol` behavior. |
| `Text` | 2 | Selects the `Text` behavior. |
| `File` | 3 | Selects the `File` behavior. |
| `Reference` | 4 | Selects the `Reference` behavior. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
