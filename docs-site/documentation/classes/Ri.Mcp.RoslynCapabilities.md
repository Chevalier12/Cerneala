# RoslynCapabilities Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the public RoslynCapabilities contract used by Roslyn Repo Indexer.

```csharp
public sealed class RoslynCapabilities
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynCapabilities(string ServerVersion, int IndexSchemaVersion, string ContractVersion, string BoundRepository, IReadOnlyList<string> Tools, IReadOnlyList<string> ResponseProfiles, IReadOnlyDictionary<string, int> Limits)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `BoundRepository` | `string` | Gets or sets the BoundRepository value. |
| `ContractVersion` | `string` | Gets or sets the ContractVersion value. |
| `IndexSchemaVersion` | `int` | Gets or sets the IndexSchemaVersion value. |
| `Limits` | `IReadOnlyDictionary<string, int>` | Gets or sets the Limits value. |
| `ResponseProfiles` | `IReadOnlyList<string>` | Gets or sets the ResponseProfiles value. |
| `ServerVersion` | `string` | Gets or sets the ServerVersion value. |
| `Tools` | `IReadOnlyList<string>` | Gets or sets the Tools value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
