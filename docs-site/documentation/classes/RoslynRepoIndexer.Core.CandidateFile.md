# CandidateFile Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the public CandidateFile contract used by Roslyn Repo Indexer.

```csharp
public sealed class CandidateFile
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `CandidateFile(string FullPath, string RelativePath, long Length, DateTime LastWriteUtc)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `FullPath` | `string` | Gets or sets the FullPath value. |
| `LastWriteUtc` | `DateTime` | Gets or sets the LastWriteUtc value. |
| `Length` | `long` | Gets or sets the Length value. |
| `RelativePath` | `string` | Gets or sets the RelativePath value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
