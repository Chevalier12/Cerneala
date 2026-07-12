# DocumentState Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the public DocumentState contract used by Roslyn Repo Indexer.

```csharp
public sealed class DocumentState
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `DocumentState(string ContentHash, long Length, DateTimeOffset LastWriteUtc, bool IsCSharp)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ContentHash` | `string` | Gets or sets the ContentHash value. |
| `IsCSharp` | `bool` | Gets or sets the IsCSharp value. |
| `LastWriteUtc` | `DateTimeOffset` | Gets or sets the LastWriteUtc value. |
| `Length` | `long` | Gets or sets the Length value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
