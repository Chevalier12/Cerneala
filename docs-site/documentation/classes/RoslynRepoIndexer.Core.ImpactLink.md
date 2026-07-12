# ImpactLink Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SemanticQueries.cs`

Represents the public ImpactLink contract used by Roslyn Repo Indexer.

```csharp
public sealed class ImpactLink
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `ImpactLink(string SymbolId, string Relationship, string Reason, double Confidence)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Confidence` | `double` | Gets or sets the Confidence value. |
| `Reason` | `string` | Gets or sets the Reason value. |
| `Relationship` | `string` | Gets or sets the Relationship value. |
| `SymbolId` | `string` | Gets or sets the SymbolId value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
