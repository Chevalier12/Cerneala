# CallGraphEdge Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SemanticQueries.cs`

Represents the public CallGraphEdge contract used by Roslyn Repo Indexer.

```csharp
public sealed class CallGraphEdge
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `CallGraphEdge(string From, string To, string Kind)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `From` | `string` | Gets or sets the From value. |
| `Kind` | `string` | Gets or sets the Kind value. |
| `To` | `string` | Gets or sets the To value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
