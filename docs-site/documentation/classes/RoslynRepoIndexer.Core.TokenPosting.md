# TokenPosting Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the public TokenPosting contract used by Roslyn Repo Indexer.

```csharp
public sealed class TokenPosting
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `TokenPosting(string Token, string Path, int Line, int Column, string Field, string Weight, string ProjectName, string DocumentId)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Column` | `int` | Gets or sets the Column value. |
| `DocumentId` | `string` | Gets or sets the DocumentId value. |
| `Field` | `string` | Gets or sets the Field value. |
| `Line` | `int` | Gets or sets the Line value. |
| `Path` | `string` | Gets or sets the Path value. |
| `ProjectName` | `string` | Gets or sets the ProjectName value. |
| `Token` | `string` | Gets or sets the Token value. |
| `Weight` | `string` | Gets or sets the Weight value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
