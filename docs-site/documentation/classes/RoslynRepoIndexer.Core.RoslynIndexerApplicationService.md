# RoslynIndexerApplicationService Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynIndexerApplicationService.cs`

Provides deterministic local operations for RoslynIndexerApplicationService scenarios.

```csharp
public sealed class RoslynIndexerApplicationService
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynIndexerApplicationService(string workingDirectory, Func<string, QueryIndex> queryIndexLoader, IndexBuilder indexBuilder)` | Initializes a new instance. |

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
| `Suggest(SuggestCommandRequest request)` | `CommandResponse<object>` | Executes the `Suggest` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
