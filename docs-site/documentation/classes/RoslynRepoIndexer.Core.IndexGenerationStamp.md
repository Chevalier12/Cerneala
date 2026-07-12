# IndexGenerationStamp Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RepositorySessions.cs`

Represents the public IndexGenerationStamp contract used by Roslyn Repo Indexer.

```csharp
public sealed class IndexGenerationStamp
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `IndexGenerationStamp(string GenerationId, DateTimeOffset UpdatedUtc, long ManifestLength)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `GenerationId` | `string` | Gets or sets the GenerationId value. |
| `ManifestLength` | `long` | Gets or sets the ManifestLength value. |
| `UpdatedUtc` | `DateTimeOffset` | Gets or sets the UpdatedUtc value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
