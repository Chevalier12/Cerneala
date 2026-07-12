# RoslynInspectRequest Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the validated input contract for RoslynInspectRequest operations.

```csharp
public sealed class RoslynInspectRequest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynInspectRequest(string RepoRoot, string Symbol, IReadOnlyList<InspectInclude> Include, int Depth, int MaxResults, int MaxChars)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Depth` | `int` | Gets or sets the Depth value. |
| `Include` | `IReadOnlyList<InspectInclude>` | Gets or sets the Include value. |
| `MaxChars` | `int` | Gets or sets the MaxChars value. |
| `MaxResults` | `int` | Gets or sets the MaxResults value. |
| `RepoRoot` | `string` | Gets or sets the RepoRoot value. |
| `Symbol` | `string` | Gets or sets the Symbol value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
