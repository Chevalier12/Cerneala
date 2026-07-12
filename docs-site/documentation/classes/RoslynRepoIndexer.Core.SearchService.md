# SearchService Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Search.cs`

Provides deterministic local operations for SearchService scenarios.

```csharp
public sealed class SearchService
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `SearchService(IndexSnapshot snapshot, SnippetReader snippets)` | Initializes a new instance. |
| `SearchService(IndexSnapshot snapshot, Func<string, int, string> readSnippet)` | Initializes a new instance. |
| `SearchService(QueryIndex queryIndex, Func<string, int, string> readSnippet)` | Initializes a new instance. |

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `Search(SearchRequest request)` | `IReadOnlyList<SearchResult>` | Executes the `Search` operation. |
| `SearchDetailed(SearchRequest request, long searchLoadMs)` | `SearchExecution` | Executes the `SearchDetailed` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
