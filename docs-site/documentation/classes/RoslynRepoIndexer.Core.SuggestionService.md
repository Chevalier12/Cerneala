# SuggestionService Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Search.cs`

Provides deterministic local operations for SuggestionService scenarios.

```csharp
public sealed class SuggestionService
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `SuggestionService(IndexSnapshot snapshot)` | Initializes a new instance. |

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `Suggest(string question, int limit)` | `IReadOnlyList<QuerySuggestion>` | Executes the `Suggest` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
