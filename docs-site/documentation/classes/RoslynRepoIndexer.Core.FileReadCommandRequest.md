# FileReadCommandRequest Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynIndexerApplicationService.cs`

Represents the validated input contract for FileReadCommandRequest operations.

```csharp
public sealed class FileReadCommandRequest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `FileReadCommandRequest(string FilePath, string ConfigPath, Nullable<long> MaxTextFileBytes)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ConfigPath` | `string` | Gets or sets the ConfigPath value. |
| `FilePath` | `string` | Gets or sets the FilePath value. |
| `MaxTextFileBytes` | `Nullable<long>` | Gets or sets the MaxTextFileBytes value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
