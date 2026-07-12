# IndexCommandRequest Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynIndexerApplicationService.cs`

Represents the validated input contract for IndexCommandRequest operations.

```csharp
public sealed class IndexCommandRequest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `IndexCommandRequest(string Path, bool Force, bool IncludeGenerated, Nullable<bool> IncludeNonCSharpText, Nullable<long> MaxTextFileBytes, Nullable<int> MaxDegreeOfParallelism, string ConfigPath)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ConfigPath` | `string` | Gets or sets the ConfigPath value. |
| `Force` | `bool` | Gets or sets the Force value. |
| `IncludeGenerated` | `bool` | Gets or sets the IncludeGenerated value. |
| `IncludeNonCSharpText` | `Nullable<bool>` | Gets or sets the IncludeNonCSharpText value. |
| `MaxDegreeOfParallelism` | `Nullable<int>` | Gets or sets the MaxDegreeOfParallelism value. |
| `MaxTextFileBytes` | `Nullable<long>` | Gets or sets the MaxTextFileBytes value. |
| `Path` | `string` | Gets or sets the Path value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
