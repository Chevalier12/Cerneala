# SearchScorer Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Search.cs`

Represents the public SearchScorer contract used by Roslyn Repo Indexer.

```csharp
public sealed class SearchScorer
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `ScoreSymbol(string fullyQualifiedName, string name, string query)` | `double` | Executes the `ScoreSymbol` operation. |
| `ScoreSymbolMatch(string fullyQualifiedName, string name, string query)` | `SearchScore` | Executes the `ScoreSymbolMatch` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
