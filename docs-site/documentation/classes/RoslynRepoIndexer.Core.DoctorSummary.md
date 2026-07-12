# DoctorSummary Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the structured output contract for DoctorSummary operations.

```csharp
public sealed class DoctorSummary
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `DoctorSummary(string RepoRoot, IReadOnlyList<DoctorCheck> Checks)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Checks` | `IReadOnlyList<DoctorCheck>` | Gets or sets the Checks value. |
| `RepoRoot` | `string` | Gets or sets the RepoRoot value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
