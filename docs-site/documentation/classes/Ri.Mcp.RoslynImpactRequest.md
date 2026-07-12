# RoslynImpactRequest Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the validated input contract for RoslynImpactRequest operations.

```csharp
public sealed class RoslynImpactRequest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynImpactRequest(string RepoRoot, string Symbol, int MaxResults)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `MaxResults` | `int` | Gets or sets the MaxResults value. |
| `RepoRoot` | `string` | Gets or sets the RepoRoot value. |
| `Symbol` | `string` | Gets or sets the Symbol value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
