# SearchExecution Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the public SearchExecution contract used by Roslyn Repo Indexer.

```csharp
public sealed class SearchExecution
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `SearchExecution(IReadOnlyList<SearchResult> Results, bool TimedOut, long SearchLoadMs, long SearchScoreMs)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Results` | `IReadOnlyList<SearchResult>` | Gets or sets the Results value. |
| `SearchLoadMs` | `long` | Gets or sets the SearchLoadMs value. |
| `SearchScoreMs` | `long` | Gets or sets the SearchScoreMs value. |
| `TimedOut` | `bool` | Gets or sets the TimedOut value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
