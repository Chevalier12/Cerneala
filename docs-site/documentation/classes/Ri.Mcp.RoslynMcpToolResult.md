# RoslynMcpToolResult Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the structured output contract for RoslynMcpToolResult operations.

```csharp
public sealed class RoslynMcpToolResult<T>
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynMcpToolResult(bool Success, string Tool, string RepoRoot, long ElapsedMs, IReadOnlyList<string> Warnings, IReadOnlyList<RoslynMcpError> Errors, T Data, int ExitCode, Nullable<DateTimeOffset> IndexUpdatedUtc, RoslynMcpCacheInfo Cache, bool Truncated, string ContinuationToken)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Cache` | `RoslynMcpCacheInfo` | Gets or sets the Cache value. |
| `ContinuationToken` | `string` | Gets or sets the ContinuationToken value. |
| `Data` | `T` | Gets or sets the Data value. |
| `ElapsedMs` | `long` | Gets or sets the ElapsedMs value. |
| `Errors` | `IReadOnlyList<RoslynMcpError>` | Gets or sets the Errors value. |
| `ExitCode` | `int` | Gets or sets the ExitCode value. |
| `IndexUpdatedUtc` | `Nullable<DateTimeOffset>` | Gets or sets the IndexUpdatedUtc value. |
| `RepoRoot` | `string` | Gets or sets the RepoRoot value. |
| `Success` | `bool` | Gets or sets the Success value. |
| `Tool` | `string` | Gets or sets the Tool value. |
| `Truncated` | `bool` | Gets or sets the Truncated value. |
| `Warnings` | `IReadOnlyList<string>` | Gets or sets the Warnings value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
