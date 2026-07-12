# RoslynReadRequest Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the validated input contract for RoslynReadRequest operations.

```csharp
public sealed class RoslynReadRequest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynReadRequest(string RepoRoot, string FilePath, string ConfigPath, Nullable<long> MaxTextFileBytes, int MaxChars)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ConfigPath` | `string` | Gets or sets the ConfigPath value. |
| `FilePath` | `string` | Gets or sets the FilePath value. |
| `MaxChars` | `int` | Gets or sets the MaxChars value. |
| `MaxTextFileBytes` | `Nullable<long>` | Gets or sets the MaxTextFileBytes value. |
| `RepoRoot` | `string` | Gets or sets the RepoRoot value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
