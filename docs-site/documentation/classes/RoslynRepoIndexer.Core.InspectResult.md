# InspectResult Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SemanticQueries.cs`

Represents the structured output contract for InspectResult operations.

```csharp
public sealed class InspectResult
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `InspectResult(SymbolSummary Symbol, string Source, string Documentation, SymbolSummary ContainingType, IReadOnlyList<SymbolSummary> BaseTypes, IReadOnlyList<SymbolSummary> Members, IReadOnlyList<SymbolSummary> Callers, IReadOnlyList<SymbolSummary> Callees, IReadOnlyList<SourceSpanSummary> References, IReadOnlyList<SymbolSummary> Implementations, IReadOnlyList<TestCandidate> Tests, bool Truncated)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `BaseTypes` | `IReadOnlyList<SymbolSummary>` | Gets or sets the BaseTypes value. |
| `Callees` | `IReadOnlyList<SymbolSummary>` | Gets or sets the Callees value. |
| `Callers` | `IReadOnlyList<SymbolSummary>` | Gets or sets the Callers value. |
| `ContainingType` | `SymbolSummary` | Gets or sets the ContainingType value. |
| `Documentation` | `string` | Gets or sets the Documentation value. |
| `Implementations` | `IReadOnlyList<SymbolSummary>` | Gets or sets the Implementations value. |
| `Members` | `IReadOnlyList<SymbolSummary>` | Gets or sets the Members value. |
| `References` | `IReadOnlyList<SourceSpanSummary>` | Gets or sets the References value. |
| `Source` | `string` | Gets or sets the Source value. |
| `Symbol` | `SymbolSummary` | Gets or sets the Symbol value. |
| `Tests` | `IReadOnlyList<TestCandidate>` | Gets or sets the Tests value. |
| `Truncated` | `bool` | Gets or sets the Truncated value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
