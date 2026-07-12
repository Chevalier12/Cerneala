# LoadedWorkspace Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynIndexing.cs`

Represents the public LoadedWorkspace contract used by Roslyn Repo Indexer.

```csharp
public sealed class LoadedWorkspace
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Solution` | `unknown` | Gets the Solution value. |

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `Dispose()` | `void` | Executes the `Dispose` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
