# IndexSummary Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the structured output contract for IndexSummary operations.

```csharp
public sealed class IndexSummary
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `IndexSummary(string RepoRoot, int Documents, int Symbols, int References, int Tokens, int Warnings, TimeSpan Duration, bool FullRebuild, bool Incremental, int DirtyDocuments, int DeletedDocuments, int UnchangedDocuments, IndexTimingSummary Timings)` | Initializes a new instance. |
| `IndexSummary(string repoRoot, int documents, int symbols, int references, int tokens, int warnings, TimeSpan duration, bool fullRebuild, bool incremental, int dirtyDocuments, int deletedDocuments, int unchangedDocuments)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `DeletedDocuments` | `int` | Gets or sets the DeletedDocuments value. |
| `DirtyDocuments` | `int` | Gets or sets the DirtyDocuments value. |
| `Documents` | `int` | Gets or sets the Documents value. |
| `Duration` | `TimeSpan` | Gets or sets the Duration value. |
| `FullRebuild` | `bool` | Gets or sets the FullRebuild value. |
| `Incremental` | `bool` | Gets or sets the Incremental value. |
| `References` | `int` | Gets or sets the References value. |
| `RepoRoot` | `string` | Gets or sets the RepoRoot value. |
| `Symbols` | `int` | Gets or sets the Symbols value. |
| `Timings` | `IndexTimingSummary` | Gets or sets the Timings value. |
| `Tokens` | `int` | Gets or sets the Tokens value. |
| `TotalMs` | `long` | Gets the TotalMs value. |
| `UnchangedDocuments` | `int` | Gets or sets the UnchangedDocuments value. |
| `Warnings` | `int` | Gets or sets the Warnings value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
