# DocumentEntry Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the structured output contract for DocumentEntry operations.

```csharp
public sealed class DocumentEntry
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `DocumentEntry(string DocumentId, string ProjectId, string RelativePath, string ProjectName, string Language, bool IsCSharp, bool IsGenerated, bool IsNonCSharpText, long LengthBytes, DateTimeOffset LastWriteUtc, string ContentHash, string DeclarationHash, int LineCount)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ContentHash` | `string` | Gets or sets the ContentHash value. |
| `DeclarationHash` | `string` | Gets or sets the DeclarationHash value. |
| `DocumentId` | `string` | Gets or sets the DocumentId value. |
| `IsCSharp` | `bool` | Gets or sets the IsCSharp value. |
| `IsGenerated` | `bool` | Gets or sets the IsGenerated value. |
| `IsNonCSharpText` | `bool` | Gets or sets the IsNonCSharpText value. |
| `Language` | `string` | Gets or sets the Language value. |
| `LastWriteUtc` | `DateTimeOffset` | Gets or sets the LastWriteUtc value. |
| `LengthBytes` | `long` | Gets or sets the LengthBytes value. |
| `LineCount` | `int` | Gets or sets the LineCount value. |
| `ProjectId` | `string` | Gets or sets the ProjectId value. |
| `ProjectName` | `string` | Gets or sets the ProjectName value. |
| `RelativePath` | `string` | Gets or sets the RelativePath value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
