# ConfigLoadResult Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the structured output contract for ConfigLoadResult operations.

```csharp
public sealed class ConfigLoadResult
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `ConfigLoadResult(IndexerConfig Config, IReadOnlyList<string> Warnings)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Config` | `IndexerConfig` | Gets or sets the Config value. |
| `Warnings` | `IReadOnlyList<string>` | Gets or sets the Warnings value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
