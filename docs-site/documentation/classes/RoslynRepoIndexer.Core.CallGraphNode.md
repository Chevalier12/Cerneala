# CallGraphNode Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SemanticQueries.cs`

Represents the public CallGraphNode contract used by Roslyn Repo Indexer.

```csharp
public sealed class CallGraphNode
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `CallGraphNode(string Id, string Name, string Kind, string Path, Nullable<int> Line, bool External)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `External` | `bool` | Gets or sets the External value. |
| `Id` | `string` | Gets or sets the Id value. |
| `Kind` | `string` | Gets or sets the Kind value. |
| `Line` | `Nullable<int>` | Gets or sets the Line value. |
| `Name` | `string` | Gets or sets the Name value. |
| `Path` | `string` | Gets or sets the Path value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
