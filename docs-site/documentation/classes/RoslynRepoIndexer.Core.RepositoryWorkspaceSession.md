# RepositoryWorkspaceSession Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RepositorySessions.cs`

Represents the public RepositoryWorkspaceSession contract used by Roslyn Repo Indexer.

```csharp
public sealed class RepositoryWorkspaceSession
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RepositoryWorkspaceSession()` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsLoaded` | `bool` | Gets the IsLoaded value. |

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `Dispose()` | `void` | Executes the `Dispose` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
