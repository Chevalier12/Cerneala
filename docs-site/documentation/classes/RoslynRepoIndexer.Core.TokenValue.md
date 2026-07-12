# TokenValue Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the public TokenValue contract used by Roslyn Repo Indexer.

```csharp
public sealed class TokenValue
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `TokenValue(string Value, int Line, int Column)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Column` | `int` | Gets or sets the Column value. |
| `Line` | `int` | Gets or sets the Line value. |
| `Value` | `string` | Gets or sets the Value value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
