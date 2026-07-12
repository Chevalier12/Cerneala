# CommandResponse Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the public CommandResponse contract used by Roslyn Repo Indexer.

```csharp
public sealed class CommandResponse<T>
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `CommandResponse(bool Success, int ExitCode, T Data, IReadOnlyList<string> Warnings, IReadOnlyList<string> Errors, string Command, string Query, string RepoRoot, Nullable<long> ElapsedMs, Nullable<DateTimeOffset> IndexUpdatedUtc, T Results)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Command` | `string` | Gets or sets the Command value. |
| `Data` | `T` | Gets or sets the Data value. |
| `ElapsedMs` | `Nullable<long>` | Gets or sets the ElapsedMs value. |
| `Errors` | `IReadOnlyList<string>` | Gets or sets the Errors value. |
| `ExitCode` | `int` | Gets or sets the ExitCode value. |
| `IndexUpdatedUtc` | `Nullable<DateTimeOffset>` | Gets or sets the IndexUpdatedUtc value. |
| `Query` | `string` | Gets or sets the Query value. |
| `RepoRoot` | `string` | Gets or sets the RepoRoot value. |
| `Results` | `T` | Gets or sets the Results value. |
| `Success` | `bool` | Gets or sets the Success value. |
| `Warnings` | `IReadOnlyList<string>` | Gets or sets the Warnings value. |

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `Failure(int exitCode, IReadOnlyList<string> errors, IReadOnlyList<string> warnings, string command, string query, string repoRoot, Nullable<long> elapsedMs)` | `CommandResponse<T>` | Executes the `Failure` operation. |
| `SuccessResponse(T data, IReadOnlyList<string> warnings, string command, string query, string repoRoot, Nullable<long> elapsedMs, Nullable<DateTimeOffset> indexUpdatedUtc, bool includeResultsAlias)` | `CommandResponse<T>` | Executes the `SuccessResponse` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
