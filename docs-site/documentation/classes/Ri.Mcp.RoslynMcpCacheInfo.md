# RoslynMcpCacheInfo Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the public RoslynMcpCacheInfo contract used by Roslyn Repo Indexer.

```csharp
public sealed class RoslynMcpCacheInfo
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynMcpCacheInfo(bool SessionHit, bool GenerationReloaded, long ReloadCount, long LoadMs)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `GenerationReloaded` | `bool` | Gets or sets the GenerationReloaded value. |
| `LoadMs` | `long` | Gets or sets the LoadMs value. |
| `ReloadCount` | `long` | Gets or sets the ReloadCount value. |
| `SessionHit` | `bool` | Gets or sets the SessionHit value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
