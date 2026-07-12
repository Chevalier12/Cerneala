# RepositoryBinding Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpTools.cs`

Represents the public RepositoryBinding contract used by Roslyn Repo Indexer.

```csharp
public sealed class RepositoryBinding
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RepositoryBinding(string repoRoot)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `RepoRoot` | `string` | Gets the RepoRoot value. |

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `Resolve(string repoRoot)` | `string` | Executes the `Resolve` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
