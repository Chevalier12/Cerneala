# ImpactResult Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SemanticQueries.cs`

Represents the structured output contract for ImpactResult operations.

```csharp
public sealed class ImpactResult
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `ImpactResult(SymbolSummary Target, bool PublicApiExposure, IReadOnlyList<string> AffectedProjects, IReadOnlyList<ImpactLink> Links, IReadOnlyList<TestCandidate> Tests, bool Truncated)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `AffectedProjects` | `IReadOnlyList<string>` | Gets or sets the AffectedProjects value. |
| `Links` | `IReadOnlyList<ImpactLink>` | Gets or sets the Links value. |
| `PublicApiExposure` | `bool` | Gets or sets the PublicApiExposure value. |
| `Target` | `SymbolSummary` | Gets or sets the Target value. |
| `Tests` | `IReadOnlyList<TestCandidate>` | Gets or sets the Tests value. |
| `Truncated` | `bool` | Gets or sets the Truncated value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
