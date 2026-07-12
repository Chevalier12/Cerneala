# IRoslynIndexerApplicationService Interface

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynIndexerApplicationService.cs`

Provides deterministic local operations for IRoslynIndexerApplicationService scenarios.

```csharp
public interface IRoslynIndexerApplicationService
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `DoctorAsync(PathCommandRequest request, CancellationToken cancellationToken)` | `Task<CommandResponse<DoctorSummary>>` | Executes the `DoctorAsync` operation. |
| `Goto(SymbolQueryCommandRequest request)` | `CommandResponse<IReadOnlyList<SearchResult>>` | Executes the `Goto` operation. |
| `IndexAsync(IndexCommandRequest request, CancellationToken cancellationToken)` | `Task<CommandResponse<IndexSummary>>` | Executes the `IndexAsync` operation. |
| `PartialRead(PartialFileReadCommandRequest request)` | `CommandResponse<RepositoryPartialFileReadResult>` | Executes the `PartialRead` operation. |
| `Read(FileReadCommandRequest request)` | `CommandResponse<RepositoryFileReadResult>` | Executes the `Read` operation. |
| `RefsAsync(RefsCommandRequest request, CancellationToken cancellationToken)` | `Task<CommandResponse<object>>` | Executes the `RefsAsync` operation. |
| `Search(SearchCommandRequest request)` | `CommandResponse<IReadOnlyList<SearchResult>>` | Executes the `Search` operation. |
| `Status(PathCommandRequest request)` | `CommandResponse<StatusSummary>` | Executes the `Status` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
