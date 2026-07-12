# RoslynSuggestRequest Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the validated input contract for RoslynSuggestRequest operations.

```csharp
public sealed class RoslynSuggestRequest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynSuggestRequest(string RepoRoot, string Question, int Limit, int ExecuteTop)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ExecuteTop` | `int` | Gets or sets the ExecuteTop value. |
| `Limit` | `int` | Gets or sets the Limit value. |
| `Question` | `string` | Gets or sets the Question value. |
| `RepoRoot` | `string` | Gets or sets the RepoRoot value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
