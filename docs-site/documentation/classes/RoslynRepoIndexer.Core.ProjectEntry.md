# ProjectEntry Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the structured output contract for ProjectEntry operations.

```csharp
public sealed class ProjectEntry
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `ProjectEntry(string ProjectId, string Name, string FilePath, string Language, string TargetFramework)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `FilePath` | `string` | Gets or sets the FilePath value. |
| `Language` | `string` | Gets or sets the Language value. |
| `Name` | `string` | Gets or sets the Name value. |
| `ProjectId` | `string` | Gets or sets the ProjectId value. |
| `TargetFramework` | `string` | Gets or sets the TargetFramework value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
