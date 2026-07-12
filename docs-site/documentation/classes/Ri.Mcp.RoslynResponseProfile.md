# RoslynResponseProfile Enum

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Specifies the supported values for RoslynResponseProfile.

```csharp
public enum RoslynResponseProfile
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Compact` | 0 | Selects the `Compact` behavior. |
| `Standard` | 1 | Selects the `Standard` behavior. |
| `Diagnostic` | 2 | Selects the `Diagnostic` behavior. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
