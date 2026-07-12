# SourceSpanSummary Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SemanticQueries.cs`

Represents the structured output contract for SourceSpanSummary operations.

```csharp
public sealed class SourceSpanSummary
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `SourceSpanSummary(string Path, int Line, int Column, int EndLine, int EndColumn, int SpanStart, int SpanLength)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Column` | `int` | Gets or sets the Column value. |
| `EndColumn` | `int` | Gets or sets the EndColumn value. |
| `EndLine` | `int` | Gets or sets the EndLine value. |
| `Line` | `int` | Gets or sets the Line value. |
| `Path` | `string` | Gets or sets the Path value. |
| `SpanLength` | `int` | Gets or sets the SpanLength value. |
| `SpanStart` | `int` | Gets or sets the SpanStart value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
