# RepositoryPartialFileReadResult Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the structured output contract for RepositoryPartialFileReadResult operations.

```csharp
public sealed class RepositoryPartialFileReadResult
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RepositoryPartialFileReadResult(string FilePath, string RepoRoot, string Language, int LineCount, long SizeBytes, string ContentHash, DateTimeOffset LastModifiedUtc, bool IsIndexed, string SelectionMode, int StartLine, int EndLine, int SelectedLineCount, string Content, Nullable<int> TargetLine, Nullable<int> Context)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Content` | `string` | Gets or sets the Content value. |
| `ContentHash` | `string` | Gets or sets the ContentHash value. |
| `Context` | `Nullable<int>` | Gets or sets the Context value. |
| `EndLine` | `int` | Gets or sets the EndLine value. |
| `FilePath` | `string` | Gets or sets the FilePath value. |
| `IsIndexed` | `bool` | Gets or sets the IsIndexed value. |
| `Language` | `string` | Gets or sets the Language value. |
| `LastModifiedUtc` | `DateTimeOffset` | Gets or sets the LastModifiedUtc value. |
| `LineCount` | `int` | Gets or sets the LineCount value. |
| `RepoRoot` | `string` | Gets or sets the RepoRoot value. |
| `SelectedLineCount` | `int` | Gets or sets the SelectedLineCount value. |
| `SelectionMode` | `string` | Gets or sets the SelectionMode value. |
| `SizeBytes` | `long` | Gets or sets the SizeBytes value. |
| `StartLine` | `int` | Gets or sets the StartLine value. |
| `TargetLine` | `Nullable<int>` | Gets or sets the TargetLine value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
