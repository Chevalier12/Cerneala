# RoslynChangesRequest Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the validated input contract for RoslynChangesRequest operations.

```csharp
public sealed class RoslynChangesRequest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynChangesRequest(string RepoRoot, ChangeComparison Comparison, string BaseId, string TargetId, int MaxResults)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `BaseId` | `string` | Gets or sets the BaseId value. |
| `Comparison` | `ChangeComparison` | Gets or sets the Comparison value. |
| `MaxResults` | `int` | Gets or sets the MaxResults value. |
| `RepoRoot` | `string` | Gets or sets the RepoRoot value. |
| `TargetId` | `string` | Gets or sets the TargetId value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
