# WorkspaceLoadingException Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents an error reported by the WorkspaceLoadingException contract.

```csharp
public sealed class WorkspaceLoadingException
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `WorkspaceLoadingException(string message)` | Initializes a new instance. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
