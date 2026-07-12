# HumanOutputWriter Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynIndexing.cs`

Represents the public HumanOutputWriter contract used by Roslyn Repo Indexer.

```csharp
public sealed class HumanOutputWriter
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `HumanOutputWriter()` | Initializes a new instance. |

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `WriteSearchResults(TextWriter writer, IReadOnlyList<SearchResult> results, string emptyMessage)` | `void` | Executes the `WriteSearchResults` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
