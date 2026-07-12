# SearchResult Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the structured output contract for SearchResult operations.

```csharp
public sealed class SearchResult
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `SearchResult(string Path, int Line, int Column, int EndLine, int EndColumn, string Kind, double Score, string MatchReason, string Snippet, string SymbolId, string SymbolName, string ContainingType, string FullyQualifiedName, string ReferenceKind, string ProjectName)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Column` | `int` | Gets or sets the Column value. |
| `ContainingType` | `string` | Gets or sets the ContainingType value. |
| `EndColumn` | `int` | Gets or sets the EndColumn value. |
| `EndLine` | `int` | Gets or sets the EndLine value. |
| `FilePath` | `string` | Gets the FilePath value. |
| `FullyQualifiedName` | `string` | Gets or sets the FullyQualifiedName value. |
| `Kind` | `string` | Gets or sets the Kind value. |
| `Line` | `int` | Gets or sets the Line value. |
| `MatchReason` | `string` | Gets or sets the MatchReason value. |
| `Path` | `string` | Gets or sets the Path value. |
| `ProjectName` | `string` | Gets or sets the ProjectName value. |
| `ReferenceKind` | `string` | Gets or sets the ReferenceKind value. |
| `Score` | `double` | Gets or sets the Score value. |
| `Snippet` | `string` | Gets or sets the Snippet value. |
| `StartColumn` | `int` | Gets the StartColumn value. |
| `StartLine` | `int` | Gets the StartLine value. |
| `SymbolId` | `string` | Gets or sets the SymbolId value. |
| `SymbolName` | `string` | Gets or sets the SymbolName value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
