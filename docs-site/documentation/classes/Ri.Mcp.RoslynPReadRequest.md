# RoslynPReadRequest Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the validated input contract for RoslynPReadRequest operations.

```csharp
public sealed class RoslynPReadRequest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynPReadRequest(string RepoRoot, string FilePath, Nullable<int> StartLine, Nullable<int> EndLine, Nullable<int> AroundLine, int Context, string ConfigPath, Nullable<long> MaxTextFileBytes, int MaxChars)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `AroundLine` | `Nullable<int>` | Gets or sets the AroundLine value. |
| `ConfigPath` | `string` | Gets or sets the ConfigPath value. |
| `Context` | `int` | Gets or sets the Context value. |
| `EndLine` | `Nullable<int>` | Gets or sets the EndLine value. |
| `FilePath` | `string` | Gets or sets the FilePath value. |
| `MaxChars` | `int` | Gets or sets the MaxChars value. |
| `MaxTextFileBytes` | `Nullable<long>` | Gets or sets the MaxTextFileBytes value. |
| `RepoRoot` | `string` | Gets or sets the RepoRoot value. |
| `StartLine` | `Nullable<int>` | Gets or sets the StartLine value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
