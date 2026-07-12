# ContinuationTokenCodec Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/ContinuationTokenCodec.cs`

Represents the public ContinuationTokenCodec contract used by Roslyn Repo Indexer.

```csharp
public sealed class ContinuationTokenCodec
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `ContinuationTokenCodec()` | Initializes a new instance. |

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `Decode(string token, string expectedTool, string expectedGenerationId)` | `int` | Executes the `Decode` operation. |
| `Encode(string tool, string generationId, int offset)` | `string` | Executes the `Encode` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
