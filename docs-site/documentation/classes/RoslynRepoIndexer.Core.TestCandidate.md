# TestCandidate Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SemanticQueries.cs`

Represents the public TestCandidate contract used by Roslyn Repo Indexer.

```csharp
public sealed class TestCandidate
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `TestCandidate(string Path, string ProjectName, double Score, IReadOnlyList<string> Reasons)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Path` | `string` | Gets or sets the Path value. |
| `ProjectName` | `string` | Gets or sets the ProjectName value. |
| `Reasons` | `IReadOnlyList<string>` | Gets or sets the Reasons value. |
| `Score` | `double` | Gets or sets the Score value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
