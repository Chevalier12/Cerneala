# IndexTimingSummary Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the structured output contract for IndexTimingSummary operations.

```csharp
public sealed class IndexTimingSummary
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `IndexTimingSummary(long DiscoveryMs, long WorkspaceLoadMs, long SemanticIndexMs, long TextIndexMs, long PersistMs, long TotalMs)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `DiscoveryMs` | `long` | Gets or sets the DiscoveryMs value. |
| `PersistMs` | `long` | Gets or sets the PersistMs value. |
| `SemanticIndexMs` | `long` | Gets or sets the SemanticIndexMs value. |
| `TextIndexMs` | `long` | Gets or sets the TextIndexMs value. |
| `TotalMs` | `long` | Gets or sets the TotalMs value. |
| `WorkspaceLoadMs` | `long` | Gets or sets the WorkspaceLoadMs value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
