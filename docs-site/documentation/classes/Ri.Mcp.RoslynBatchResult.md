# RoslynBatchResult Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the structured output contract for RoslynBatchResult operations.

```csharp
public sealed class RoslynBatchResult
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynBatchResult(string GenerationId, IReadOnlyList<RoslynBatchOperationResult> Operations, bool Truncated, bool TimedOut)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `GenerationId` | `string` | Gets or sets the GenerationId value. |
| `Operations` | `IReadOnlyList<RoslynBatchOperationResult>` | Gets or sets the Operations value. |
| `TimedOut` | `bool` | Gets or sets the TimedOut value. |
| `Truncated` | `bool` | Gets or sets the Truncated value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
