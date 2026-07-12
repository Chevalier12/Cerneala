# SymbolQueryException Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SemanticQueries.cs`

Represents an error reported by the SymbolQueryException contract.

```csharp
public sealed class SymbolQueryException
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `SymbolQueryException(string code, string message, IReadOnlyList<SymbolSummary> candidates)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Candidates` | `IReadOnlyList<SymbolSummary>` | Gets the Candidates value. |
| `Code` | `string` | Gets the Code value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
