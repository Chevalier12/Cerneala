# WorkspaceInput Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the public WorkspaceInput contract used by Roslyn Repo Indexer.

```csharp
public sealed class WorkspaceInput
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `WorkspaceInput(string Path, string Kind)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Kind` | `string` | Gets or sets the Kind value. |
| `Path` | `string` | Gets or sets the Path value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
