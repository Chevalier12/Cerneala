# SuggestExecutionResponse Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynIndexerApplicationService.cs`

Represents the public SuggestExecutionResponse contract used by Roslyn Repo Indexer.

```csharp
public sealed class SuggestExecutionResponse
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `SuggestExecutionResponse(IReadOnlyList<QuerySuggestion> Suggestions, IReadOnlyList<SuggestExecutedResult> ExecutedResults)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ExecutedResults` | `IReadOnlyList<SuggestExecutedResult>` | Gets or sets the ExecutedResults value. |
| `Suggestions` | `IReadOnlyList<QuerySuggestion>` | Gets or sets the Suggestions value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
