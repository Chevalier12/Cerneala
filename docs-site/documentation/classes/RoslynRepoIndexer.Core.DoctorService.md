# DoctorService Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynIndexing.cs`

Provides deterministic local operations for DoctorService scenarios.

```csharp
public sealed class DoctorService
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `DoctorService()` | Initializes a new instance. |

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `RunAsync(string startPath, IndexerConfig config, bool deep, CancellationToken cancellationToken)` | `Task<DoctorSummary>` | Executes the `RunAsync` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
