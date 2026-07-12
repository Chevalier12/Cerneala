# ReferenceCollector Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynIndexing.cs`

Represents the public ReferenceCollector contract used by Roslyn Repo Indexer.

```csharp
public sealed class ReferenceCollector
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `ReferenceCollector()` | Initializes a new instance. |

## Methods

| Signature | Returns | Description |
| --- | --- | --- |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
