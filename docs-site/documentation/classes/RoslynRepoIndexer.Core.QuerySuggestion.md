# QuerySuggestion Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the public QuerySuggestion contract used by Roslyn Repo Indexer.

```csharp
public sealed class QuerySuggestion
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `QuerySuggestion(string Command, string Query, string Mode, double Confidence, string Reason, string ExpectedResultKind)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Command` | `string` | Gets or sets the Command value. |
| `Confidence` | `double` | Gets or sets the Confidence value. |
| `ExpectedResultKind` | `string` | Gets or sets the ExpectedResultKind value. |
| `Mode` | `string` | Gets or sets the Mode value. |
| `Query` | `string` | Gets or sets the Query value. |
| `Reason` | `string` | Gets or sets the Reason value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
