# RoslynOutlineRequest Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the validated input contract for RoslynOutlineRequest operations.

```csharp
public sealed class RoslynOutlineRequest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynOutlineRequest(string RepoRoot, string Target, int Depth, int MaxResults, int MaxChars, bool IncludePrivate, bool IncludeGenerated)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Depth` | `int` | Gets or sets the Depth value. |
| `IncludeGenerated` | `bool` | Gets or sets the IncludeGenerated value. |
| `IncludePrivate` | `bool` | Gets or sets the IncludePrivate value. |
| `MaxChars` | `int` | Gets or sets the MaxChars value. |
| `MaxResults` | `int` | Gets or sets the MaxResults value. |
| `RepoRoot` | `string` | Gets or sets the RepoRoot value. |
| `Target` | `string` | Gets or sets the Target value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
