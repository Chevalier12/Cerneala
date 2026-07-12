# SymbolSummary Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SemanticQueries.cs`

Represents the structured output contract for SymbolSummary operations.

```csharp
public sealed class SymbolSummary
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `SymbolSummary(string SymbolId, string Name, string Kind, string FullyQualifiedName, string Signature, string Accessibility, SourceSpanSummary Span, string ProjectName, string ContainerName)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Accessibility` | `string` | Gets or sets the Accessibility value. |
| `ContainerName` | `string` | Gets or sets the ContainerName value. |
| `FullyQualifiedName` | `string` | Gets or sets the FullyQualifiedName value. |
| `Kind` | `string` | Gets or sets the Kind value. |
| `Name` | `string` | Gets or sets the Name value. |
| `ProjectName` | `string` | Gets or sets the ProjectName value. |
| `Signature` | `string` | Gets or sets the Signature value. |
| `Span` | `SourceSpanSummary` | Gets or sets the Span value. |
| `SymbolId` | `string` | Gets or sets the SymbolId value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
