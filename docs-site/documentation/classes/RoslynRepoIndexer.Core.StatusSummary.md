# StatusSummary Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the structured output contract for StatusSummary operations.

```csharp
public sealed class StatusSummary
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `StatusSummary(IndexStatus Status, string RepoRoot, int SchemaVersion, int Documents, int Symbols, int References, int Tokens, int DirtyFiles, IReadOnlyList<string> Warnings)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `DirtyFiles` | `int` | Gets or sets the DirtyFiles value. |
| `Documents` | `int` | Gets or sets the Documents value. |
| `IndexState` | `string` | Gets the IndexState value. |
| `References` | `int` | Gets or sets the References value. |
| `RepoRoot` | `string` | Gets or sets the RepoRoot value. |
| `SchemaVersion` | `int` | Gets or sets the SchemaVersion value. |
| `SessionState` | `string` | Gets or sets the SessionState value. |
| `Status` | `IndexStatus` | Gets or sets the Status value. |
| `Symbols` | `int` | Gets or sets the Symbols value. |
| `Tokens` | `int` | Gets or sets the Tokens value. |
| `Warnings` | `IReadOnlyList<string>` | Gets or sets the Warnings value. |
| `WorkspaceState` | `string` | Gets or sets the WorkspaceState value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
