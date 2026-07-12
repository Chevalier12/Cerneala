# RoslynMcpError Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the public RoslynMcpError contract used by Roslyn Repo Indexer.

```csharp
public sealed class RoslynMcpError
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynMcpError(string Code, string Message, bool Retryable, string SuggestedAction)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Code` | `string` | Gets or sets the Code value. |
| `Message` | `string` | Gets or sets the Message value. |
| `Retryable` | `bool` | Gets or sets the Retryable value. |
| `SuggestedAction` | `string` | Gets or sets the SuggestedAction value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
