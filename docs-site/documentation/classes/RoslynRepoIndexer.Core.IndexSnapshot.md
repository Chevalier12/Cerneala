# IndexSnapshot Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the public IndexSnapshot contract used by Roslyn Repo Indexer.

```csharp
public sealed class IndexSnapshot
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `IndexSnapshot(IndexManifest Manifest, IReadOnlyList<DocumentEntry> Documents, IReadOnlyList<SymbolEntry> Symbols, IReadOnlyList<ReferenceEntry> References, IReadOnlyList<TokenPosting> Tokens)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Documents` | `IReadOnlyList<DocumentEntry>` | Gets or sets the Documents value. |
| `Manifest` | `IndexManifest` | Gets or sets the Manifest value. |
| `References` | `IReadOnlyList<ReferenceEntry>` | Gets or sets the References value. |
| `Symbols` | `IReadOnlyList<SymbolEntry>` | Gets or sets the Symbols value. |
| `Tokens` | `IReadOnlyList<TokenPosting>` | Gets or sets the Tokens value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
