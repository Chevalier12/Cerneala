# RoslynMcpTools Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpTools.cs`

Represents the public RoslynMcpTools contract used by Roslyn Repo Indexer.

```csharp
public sealed class RoslynMcpTools
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynMcpTools()` | Initializes a new instance. |
| `RoslynMcpTools(IRoslynIndexerApplicationService applicationService)` | Initializes a new instance. |
| `RoslynMcpTools(RepositorySessionRegistry sessionRegistry, RepositoryBinding repositoryBinding, ContinuationTokenCodec continuationTokens)` | Initializes a new instance. |

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `BatchAsync(RoslynBatchRequest request)` | `Task<RoslynMcpToolResult<object>>` | Executes the `BatchAsync` operation. |
| `CallGraphAsync(RoslynCallGraphRequest request)` | `Task<RoslynMcpToolResult<object>>` | Executes the `CallGraphAsync` operation. |
| `CapabilitiesAsync(RoslynRepoRequest request)` | `Task<RoslynMcpToolResult<RoslynCapabilities>>` | Executes the `CapabilitiesAsync` operation. |
| `ChangesAsync(RoslynChangesRequest request)` | `Task<RoslynMcpToolResult<object>>` | Executes the `ChangesAsync` operation. |
| `ContextAsync(RoslynContextRequest request)` | `Task<RoslynMcpToolResult<object>>` | Executes the `ContextAsync` operation. |
| `DoctorAsync(RoslynRepoRequest request, CancellationToken cancellationToken)` | `Task<RoslynMcpToolResult<DoctorSummary>>` | Executes the `DoctorAsync` operation. |
| `GotoAsync(RoslynGotoRequest request)` | `Task<RoslynMcpToolResult<IReadOnlyList<SearchResult>>>` | Executes the `GotoAsync` operation. |
| `ImpactAsync(RoslynImpactRequest request)` | `Task<RoslynMcpToolResult<object>>` | Executes the `ImpactAsync` operation. |
| `IndexAsync(RoslynIndexRequest request, CancellationToken cancellationToken)` | `Task<RoslynMcpToolResult<IndexSummary>>` | Executes the `IndexAsync` operation. |
| `InspectAsync(RoslynInspectRequest request)` | `Task<RoslynMcpToolResult<object>>` | Executes the `InspectAsync` operation. |
| `OutlineAsync(RoslynOutlineRequest request)` | `Task<RoslynMcpToolResult<object>>` | Executes the `OutlineAsync` operation. |
| `PReadAsync(RoslynPReadRequest request)` | `Task<RoslynMcpToolResult<RepositoryPartialFileReadResult>>` | Executes the `PReadAsync` operation. |
| `ProfileAsync(RoslynProfileRequest request)` | `Task<RoslynMcpToolResult<RoslynProfileResult>>` | Executes the `ProfileAsync` operation. |
| `ReadAsync(RoslynReadRequest request)` | `Task<RoslynMcpToolResult<RepositoryFileReadResult>>` | Executes the `ReadAsync` operation. |
| `RefsAsync(RoslynRefsRequest request, CancellationToken cancellationToken)` | `Task<RoslynMcpToolResult<object>>` | Executes the `RefsAsync` operation. |
| `SearchAsync(RoslynSearchRequest request)` | `Task<RoslynMcpToolResult<IReadOnlyList<SearchResult>>>` | Executes the `SearchAsync` operation. |
| `StatusAsync(RoslynRepoRequest request)` | `Task<RoslynMcpToolResult<StatusSummary>>` | Executes the `StatusAsync` operation. |
| `TestsForAsync(RoslynTestsForRequest request)` | `Task<RoslynMcpToolResult<object>>` | Executes the `TestsForAsync` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
