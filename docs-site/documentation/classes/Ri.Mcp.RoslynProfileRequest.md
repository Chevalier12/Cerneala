# RoslynProfileRequest Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the validated input contract for RoslynProfileRequest operations.

```csharp
public sealed class RoslynProfileRequest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynProfileRequest(string RepoRoot, int TopTerms)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `RepoRoot` | `string` | Gets or sets the RepoRoot value. |
| `TopTerms` | `int` | Gets or sets the TopTerms value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
