# SuggestCommandRequest Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynIndexerApplicationService.cs`

Represents the validated input contract for SuggestCommandRequest operations.

```csharp
public sealed class SuggestCommandRequest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `SuggestCommandRequest(string Question, int Limit, int ExecuteTop)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ExecuteTop` | `int` | Gets or sets the ExecuteTop value. |
| `Limit` | `int` | Gets or sets the Limit value. |
| `Question` | `string` | Gets or sets the Question value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
