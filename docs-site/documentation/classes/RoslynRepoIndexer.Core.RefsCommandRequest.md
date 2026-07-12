# RefsCommandRequest Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynIndexerApplicationService.cs`

Represents the validated input contract for RefsCommandRequest operations.

```csharp
public sealed class RefsCommandRequest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RefsCommandRequest(string Query, string SymbolId, bool Exact, Nullable<int> TimeoutSeconds, int Limit)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Exact` | `bool` | Gets or sets the Exact value. |
| `Limit` | `int` | Gets or sets the Limit value. |
| `Query` | `string` | Gets or sets the Query value. |
| `SymbolId` | `string` | Gets or sets the SymbolId value. |
| `TimeoutSeconds` | `Nullable<int>` | Gets or sets the TimeoutSeconds value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
