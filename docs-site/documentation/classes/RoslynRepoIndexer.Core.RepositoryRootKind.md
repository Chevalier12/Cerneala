# RepositoryRootKind Enum

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Specifies the supported values for RepositoryRootKind.

```csharp
public enum RepositoryRootKind
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Git` | 0 | Selects the `Git` behavior. |
| `WorkspaceFile` | 1 | Selects the `WorkspaceFile` behavior. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
