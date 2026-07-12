# RoslynBatchRequest Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the validated input contract for RoslynBatchRequest operations.

```csharp
public sealed class RoslynBatchRequest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynBatchRequest(string RepoRoot, IReadOnlyList<RoslynBatchOperation> Operations, RoslynBatchFailureMode FailureMode, int MaxChars, int TimeoutMs)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `FailureMode` | `RoslynBatchFailureMode` | Gets or sets the FailureMode value. |
| `MaxChars` | `int` | Gets or sets the MaxChars value. |
| `Operations` | `IReadOnlyList<RoslynBatchOperation>` | Gets or sets the Operations value. |
| `RepoRoot` | `string` | Gets or sets the RepoRoot value. |
| `TimeoutMs` | `int` | Gets or sets the TimeoutMs value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
