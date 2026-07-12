# RepositoryFileReadException Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents an error reported by the RepositoryFileReadException contract.

```csharp
public sealed class RepositoryFileReadException
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RepositoryFileReadException(string message, int exitCode)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ExitCode` | `int` | Gets the ExitCode value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
