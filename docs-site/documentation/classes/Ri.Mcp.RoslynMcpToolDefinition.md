# RoslynMcpToolDefinition Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the public RoslynMcpToolDefinition contract used by Roslyn Repo Indexer.

```csharp
public sealed class RoslynMcpToolDefinition
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynMcpToolDefinition(string Name, string Description, string InputSchemaJson)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Description` | `string` | Gets or sets the Description value. |
| `InputSchemaJson` | `string` | Gets or sets the InputSchemaJson value. |
| `Name` | `string` | Gets or sets the Name value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
