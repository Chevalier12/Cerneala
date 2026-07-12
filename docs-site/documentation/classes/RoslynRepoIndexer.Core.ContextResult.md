# ContextResult Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SemanticQueries.cs`

Represents the structured output contract for ContextResult operations.

```csharp
public sealed class ContextResult
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `ContextResult(SymbolSummary Symbol, string Source, IReadOnlyList<SymbolSummary> Related, IReadOnlyList<TestCandidate> Tests, bool Truncated)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Related` | `IReadOnlyList<SymbolSummary>` | Gets or sets the Related value. |
| `Source` | `string` | Gets or sets the Source value. |
| `Symbol` | `SymbolSummary` | Gets or sets the Symbol value. |
| `Tests` | `IReadOnlyList<TestCandidate>` | Gets or sets the Tests value. |
| `Truncated` | `bool` | Gets or sets the Truncated value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
