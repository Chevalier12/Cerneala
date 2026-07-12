# ParsedQuery Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the public ParsedQuery contract used by Roslyn Repo Indexer.

```csharp
public sealed class ParsedQuery
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `ParsedQuery(IReadOnlyList<string> Terms, IReadOnlyList<string> Phrases, IReadOnlyDictionary<string, string> Filters)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Filters` | `IReadOnlyDictionary<string, string>` | Gets or sets the Filters value. |
| `Phrases` | `IReadOnlyList<string>` | Gets or sets the Phrases value. |
| `Terms` | `IReadOnlyList<string>` | Gets or sets the Terms value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
