# OutlineItem Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SemanticQueries.cs`

Represents the public OutlineItem contract used by Roslyn Repo Indexer.

```csharp
public sealed class OutlineItem
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `OutlineItem(SymbolSummary Symbol, string ParentSymbolId, int Depth)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Depth` | `int` | Gets or sets the Depth value. |
| `ParentSymbolId` | `string` | Gets or sets the ParentSymbolId value. |
| `Symbol` | `SymbolSummary` | Gets or sets the Symbol value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
