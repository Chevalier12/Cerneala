# RoslynCallGraphRequest Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the validated input contract for RoslynCallGraphRequest operations.

```csharp
public sealed class RoslynCallGraphRequest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynCallGraphRequest(string RepoRoot, string Symbol, CallGraphDirection Direction, int Depth, int MaxNodes, bool IncludeTests, bool IncludeExternal)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Depth` | `int` | Gets or sets the Depth value. |
| `Direction` | `CallGraphDirection` | Gets or sets the Direction value. |
| `IncludeExternal` | `bool` | Gets or sets the IncludeExternal value. |
| `IncludeTests` | `bool` | Gets or sets the IncludeTests value. |
| `MaxNodes` | `int` | Gets or sets the MaxNodes value. |
| `RepoRoot` | `string` | Gets or sets the RepoRoot value. |
| `Symbol` | `string` | Gets or sets the Symbol value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
