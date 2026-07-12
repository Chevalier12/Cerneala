# SemanticChangesResult Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SemanticChanges.cs`

Represents the structured output contract for SemanticChangesResult operations.

```csharp
public sealed class SemanticChangesResult
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `SemanticChangesResult(string Comparison, string BaseId, string TargetId, IReadOnlyList<ChangedFile> Files, IReadOnlyList<ChangedSymbol> Symbols, IReadOnlyList<string> AffectedProjects, bool Truncated)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `AffectedProjects` | `IReadOnlyList<string>` | Gets or sets the AffectedProjects value. |
| `BaseId` | `string` | Gets or sets the BaseId value. |
| `Comparison` | `string` | Gets or sets the Comparison value. |
| `Files` | `IReadOnlyList<ChangedFile>` | Gets or sets the Files value. |
| `Symbols` | `IReadOnlyList<ChangedSymbol>` | Gets or sets the Symbols value. |
| `TargetId` | `string` | Gets or sets the TargetId value. |
| `Truncated` | `bool` | Gets or sets the Truncated value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
