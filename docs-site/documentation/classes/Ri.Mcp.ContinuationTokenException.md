# ContinuationTokenException Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/ContinuationTokenCodec.cs`

Represents an error reported by the ContinuationTokenException contract.

```csharp
public sealed class ContinuationTokenException
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `ContinuationTokenException(string message)` | Initializes a new instance. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
