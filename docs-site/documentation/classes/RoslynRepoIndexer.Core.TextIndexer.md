# TextIndexer Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynIndexing.cs`

Represents the public TextIndexer contract used by Roslyn Repo Indexer.

```csharp
public sealed class TextIndexer
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `TextIndexer()` | Initializes a new instance. |

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `IndexText(string relativePath, string text, string projectName, string documentId)` | `IReadOnlyList<TokenPosting>` | Executes the `IndexText` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
