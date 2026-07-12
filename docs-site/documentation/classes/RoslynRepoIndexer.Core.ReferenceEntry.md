# ReferenceEntry Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the structured output contract for ReferenceEntry operations.

```csharp
public sealed class ReferenceEntry
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `ReferenceEntry(string ReferenceId, string SymbolId, string DocumentId, string ProjectId, string ReferencedName, string Path, int Line, int Column, int EndLine, int EndColumn, int SpanStart, int SpanLength, string ProjectName, string ReferenceKind, string ContainingSymbolId, bool IsInvocation)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Column` | `int` | Gets or sets the Column value. |
| `ContainingSymbolId` | `string` | Gets or sets the ContainingSymbolId value. |
| `DocumentId` | `string` | Gets or sets the DocumentId value. |
| `EndColumn` | `int` | Gets or sets the EndColumn value. |
| `EndLine` | `int` | Gets or sets the EndLine value. |
| `IsInvocation` | `bool` | Gets or sets the IsInvocation value. |
| `Line` | `int` | Gets or sets the Line value. |
| `Path` | `string` | Gets or sets the Path value. |
| `ProjectId` | `string` | Gets or sets the ProjectId value. |
| `ProjectName` | `string` | Gets or sets the ProjectName value. |
| `ReferencedName` | `string` | Gets or sets the ReferencedName value. |
| `ReferenceId` | `string` | Gets or sets the ReferenceId value. |
| `ReferenceKind` | `string` | Gets or sets the ReferenceKind value. |
| `SpanLength` | `int` | Gets or sets the SpanLength value. |
| `SpanStart` | `int` | Gets or sets the SpanStart value. |
| `SymbolId` | `string` | Gets or sets the SymbolId value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
