# PathCommandRequest Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynIndexerApplicationService.cs`

Represents the validated input contract for PathCommandRequest operations.

```csharp
public sealed class PathCommandRequest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `PathCommandRequest(string Path, string ConfigPath, bool Deep)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ConfigPath` | `string` | Gets or sets the ConfigPath value. |
| `Deep` | `bool` | Gets or sets the Deep value. |
| `Path` | `string` | Gets or sets the Path value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
