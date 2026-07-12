# RoslynMcpToolCatalog Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpToolCatalog.cs`

Represents the public RoslynMcpToolCatalog contract used by Roslyn Repo Indexer.

```csharp
public sealed class RoslynMcpToolCatalog
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Tools` | `IReadOnlyList<RoslynMcpToolDefinition>` | Gets the Tools value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
