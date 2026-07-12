# ChangedSymbol Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SemanticChanges.cs`

Represents the public ChangedSymbol contract used by Roslyn Repo Indexer.

```csharp
public sealed class ChangedSymbol
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `ChangedSymbol(SemanticChangeKind Kind, string SymbolId, SymbolSummary Before, SymbolSummary After, bool SignatureChanged, bool PublicApiChange)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `After` | `SymbolSummary` | Gets or sets the After value. |
| `Before` | `SymbolSummary` | Gets or sets the Before value. |
| `Kind` | `SemanticChangeKind` | Gets or sets the Kind value. |
| `PublicApiChange` | `bool` | Gets or sets the PublicApiChange value. |
| `SignatureChanged` | `bool` | Gets or sets the SignatureChanged value. |
| `SymbolId` | `string` | Gets or sets the SymbolId value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
