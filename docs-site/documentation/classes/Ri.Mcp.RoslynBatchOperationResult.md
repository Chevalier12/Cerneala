# RoslynBatchOperationResult Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the structured output contract for RoslynBatchOperationResult operations.

```csharp
public sealed class RoslynBatchOperationResult
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynBatchOperationResult(string Id, string Operation, bool Success, object Data, RoslynMcpError Error)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Data` | `object` | Gets or sets the Data value. |
| `Error` | `RoslynMcpError` | Gets or sets the Error value. |
| `Id` | `string` | Gets or sets the Id value. |
| `Operation` | `string` | Gets or sets the Operation value. |
| `Success` | `bool` | Gets or sets the Success value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
