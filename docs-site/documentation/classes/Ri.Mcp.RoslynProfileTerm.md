# RoslynProfileTerm Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the public RoslynProfileTerm contract used by Roslyn Repo Indexer.

```csharp
public sealed class RoslynProfileTerm
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynProfileTerm(string Term, int PostingCount)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `PostingCount` | `int` | Gets or sets the PostingCount value. |
| `Term` | `string` | Gets or sets the Term value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
