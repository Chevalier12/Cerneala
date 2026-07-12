# SuggestExecutedResult Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynIndexerApplicationService.cs`

Represents the structured output contract for SuggestExecutedResult operations.

```csharp
public sealed class SuggestExecutedResult
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `SuggestExecutedResult(QuerySuggestion Suggestion, IReadOnlyList<SearchResult> Results)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Results` | `IReadOnlyList<SearchResult>` | Gets or sets the Results value. |
| `Suggestion` | `QuerySuggestion` | Gets or sets the Suggestion value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
