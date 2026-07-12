# RoslynBatchOperation Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the public RoslynBatchOperation contract used by Roslyn Repo Indexer.

```csharp
public sealed class RoslynBatchOperation
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynBatchOperation(string Id, string Operation, string Query, string SymbolFrom, string FileFrom, IReadOnlyList<InspectInclude> Include, int Depth, int Limit)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Depth` | `int` | Gets or sets the Depth value. |
| `FileFrom` | `string` | Gets or sets the FileFrom value. |
| `Id` | `string` | Gets or sets the Id value. |
| `Include` | `IReadOnlyList<InspectInclude>` | Gets or sets the Include value. |
| `Limit` | `int` | Gets or sets the Limit value. |
| `Operation` | `string` | Gets or sets the Operation value. |
| `Query` | `string` | Gets or sets the Query value. |
| `SymbolFrom` | `string` | Gets or sets the SymbolFrom value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
