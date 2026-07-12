# RoslynIndexRequest Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the validated input contract for RoslynIndexRequest operations.

```csharp
public sealed class RoslynIndexRequest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynIndexRequest(string RepoRoot, bool Force, bool IncludeGenerated, Nullable<bool> IncludeNonCSharpText, Nullable<long> MaxTextFileBytes, Nullable<int> MaxDegreeOfParallelism, string ConfigPath)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ConfigPath` | `string` | Gets or sets the ConfigPath value. |
| `Force` | `bool` | Gets or sets the Force value. |
| `IncludeGenerated` | `bool` | Gets or sets the IncludeGenerated value. |
| `IncludeNonCSharpText` | `Nullable<bool>` | Gets or sets the IncludeNonCSharpText value. |
| `MaxDegreeOfParallelism` | `Nullable<int>` | Gets or sets the MaxDegreeOfParallelism value. |
| `MaxTextFileBytes` | `Nullable<long>` | Gets or sets the MaxTextFileBytes value. |
| `RepoRoot` | `string` | Gets or sets the RepoRoot value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
