# CallGraphResult Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SemanticQueries.cs`

Represents the structured output contract for CallGraphResult operations.

```csharp
public sealed class CallGraphResult
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `CallGraphResult(IReadOnlyList<CallGraphNode> Nodes, IReadOnlyList<CallGraphEdge> Edges, bool Truncated)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Edges` | `IReadOnlyList<CallGraphEdge>` | Gets or sets the Edges value. |
| `Nodes` | `IReadOnlyList<CallGraphNode>` | Gets or sets the Nodes value. |
| `Truncated` | `bool` | Gets or sets the Truncated value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
