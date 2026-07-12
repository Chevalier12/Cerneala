# RoslynGotoRequest Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the validated input contract for RoslynGotoRequest operations.

```csharp
public sealed class RoslynGotoRequest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynGotoRequest(string RepoRoot, string Query, string SymbolId, string Kind, int Limit, RoslynResponseProfile Profile, string ContinuationToken)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ContinuationToken` | `string` | Gets or sets the ContinuationToken value. |
| `Kind` | `string` | Gets or sets the Kind value. |
| `Limit` | `int` | Gets or sets the Limit value. |
| `Profile` | `RoslynResponseProfile` | Gets or sets the Profile value. |
| `Query` | `string` | Gets or sets the Query value. |
| `RepoRoot` | `string` | Gets or sets the RepoRoot value. |
| `SymbolId` | `string` | Gets or sets the SymbolId value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
