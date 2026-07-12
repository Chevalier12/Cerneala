# RepositorySessionRegistry Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RepositorySessions.cs`

Represents the public RepositorySessionRegistry contract used by Roslyn Repo Indexer.

```csharp
public sealed class RepositorySessionRegistry
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RepositorySessionRegistry(int maxSessions)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Count` | `int` | Gets the Count value. |

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `Contains(string repoRoot)` | `bool` | Executes the `Contains` operation. |
| `Get(string repoRoot)` | `RepositoryIndexSession` | Executes the `Get` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
