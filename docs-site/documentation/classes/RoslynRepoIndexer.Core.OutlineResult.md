# OutlineResult Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SemanticQueries.cs`

Represents the structured output contract for OutlineResult operations.

```csharp
public sealed class OutlineResult
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `OutlineResult(IReadOnlyList<OutlineItem> Items, bool Truncated)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Items` | `IReadOnlyList<OutlineItem>` | Gets or sets the Items value. |
| `Truncated` | `bool` | Gets or sets the Truncated value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
