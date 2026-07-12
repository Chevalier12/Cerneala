# SymbolIdProvider Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynIndexing.cs`

Represents the public SymbolIdProvider contract used by Roslyn Repo Indexer.

```csharp
public sealed class SymbolIdProvider
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `SymbolIdProvider()` | Initializes a new instance. |

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `Create(string kind, string fullyQualifiedName, string signature, string path, int line, int column)` | `string` | Executes the `Create` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
