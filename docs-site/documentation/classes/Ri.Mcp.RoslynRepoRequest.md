# RoslynRepoRequest Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the validated input contract for RoslynRepoRequest operations.

```csharp
public sealed class RoslynRepoRequest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynRepoRequest(string RepoRoot, string ConfigPath, bool Deep)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ConfigPath` | `string` | Gets or sets the ConfigPath value. |
| `Deep` | `bool` | Gets or sets the Deep value. |
| `RepoRoot` | `string` | Gets or sets the RepoRoot value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
