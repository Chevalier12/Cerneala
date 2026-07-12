# RoslynProfileResult Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the structured output contract for RoslynProfileResult operations.

```csharp
public sealed class RoslynProfileResult
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynProfileResult(string GenerationId, RepositorySessionMetrics Session, IndexTimingSummary IndexTimings, IReadOnlyList<RoslynProfileFileSize> Files, IReadOnlyList<RoslynProfileTerm> TopTerms, long TotalIndexBytes)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Files` | `IReadOnlyList<RoslynProfileFileSize>` | Gets or sets the Files value. |
| `GenerationId` | `string` | Gets or sets the GenerationId value. |
| `IndexTimings` | `IndexTimingSummary` | Gets or sets the IndexTimings value. |
| `Session` | `RepositorySessionMetrics` | Gets or sets the Session value. |
| `TopTerms` | `IReadOnlyList<RoslynProfileTerm>` | Gets or sets the TopTerms value. |
| `TotalIndexBytes` | `long` | Gets or sets the TotalIndexBytes value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
