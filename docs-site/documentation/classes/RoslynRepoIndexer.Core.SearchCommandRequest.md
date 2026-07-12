# SearchCommandRequest Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynIndexerApplicationService.cs`

Represents the validated input contract for SearchCommandRequest operations.

```csharp
public sealed class SearchCommandRequest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `SearchCommandRequest(string Query, SearchMode Mode, int Limit, string Kind, string Path, string Project, Nullable<bool> IncludeTests, string FromFile, string FromProject, Nullable<int> TimeoutMs)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `FromFile` | `string` | Gets or sets the FromFile value. |
| `FromProject` | `string` | Gets or sets the FromProject value. |
| `IncludeTests` | `Nullable<bool>` | Gets or sets the IncludeTests value. |
| `Kind` | `string` | Gets or sets the Kind value. |
| `Limit` | `int` | Gets or sets the Limit value. |
| `Mode` | `SearchMode` | Gets or sets the Mode value. |
| `Path` | `string` | Gets or sets the Path value. |
| `Project` | `string` | Gets or sets the Project value. |
| `Query` | `string` | Gets or sets the Query value. |
| `TimeoutMs` | `Nullable<int>` | Gets or sets the TimeoutMs value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
