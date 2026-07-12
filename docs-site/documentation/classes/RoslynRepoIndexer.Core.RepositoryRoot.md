# RepositoryRoot Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the public RepositoryRoot contract used by Roslyn Repo Indexer.

```csharp
public sealed class RepositoryRoot
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RepositoryRoot(string RootPath, RepositoryRootKind Kind)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Kind` | `RepositoryRootKind` | Gets or sets the Kind value. |
| `RootPath` | `string` | Gets or sets the RootPath value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
