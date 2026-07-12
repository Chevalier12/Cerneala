# PartialFileReadCommandRequest Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynIndexerApplicationService.cs`

Represents the validated input contract for PartialFileReadCommandRequest operations.

```csharp
public sealed class PartialFileReadCommandRequest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `PartialFileReadCommandRequest(string FilePath, Nullable<int> StartLine, Nullable<int> EndLine, Nullable<int> AroundLine, int Context, string ConfigPath, Nullable<long> MaxTextFileBytes)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `AroundLine` | `Nullable<int>` | Gets or sets the AroundLine value. |
| `ConfigPath` | `string` | Gets or sets the ConfigPath value. |
| `Context` | `int` | Gets or sets the Context value. |
| `EndLine` | `Nullable<int>` | Gets or sets the EndLine value. |
| `FilePath` | `string` | Gets or sets the FilePath value. |
| `MaxTextFileBytes` | `Nullable<long>` | Gets or sets the MaxTextFileBytes value. |
| `StartLine` | `Nullable<int>` | Gets or sets the StartLine value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
