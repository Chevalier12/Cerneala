# RoslynSearchRequest Class

## Definition
Namespace: `Ri.Mcp`

Assembly/Project: `Ri.Mcp`

Source: `Tools/RoslynRepoIndexer/src/Ri.Mcp/RoslynMcpContracts.cs`

Represents the validated input contract for RoslynSearchRequest operations.

```csharp
public sealed class RoslynSearchRequest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `RoslynSearchRequest(string RepoRoot, string Query, string Mode, int Limit, string Kind, string Path, string Project, Nullable<bool> IncludeTests, string FromFile, string FromProject, Nullable<int> TimeoutMs, RoslynResponseProfile Profile, string ContinuationToken)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ContinuationToken` | `string` | Gets or sets the ContinuationToken value. |
| `FromFile` | `string` | Gets or sets the FromFile value. |
| `FromProject` | `string` | Gets or sets the FromProject value. |
| `IncludeTests` | `Nullable<bool>` | Gets or sets the IncludeTests value. |
| `Kind` | `string` | Gets or sets the Kind value. |
| `Limit` | `int` | Gets or sets the Limit value. |
| `Mode` | `string` | Gets or sets the Mode value. |
| `Path` | `string` | Gets or sets the Path value. |
| `Profile` | `RoslynResponseProfile` | Gets or sets the Profile value. |
| `Project` | `string` | Gets or sets the Project value. |
| `Query` | `string` | Gets or sets the Query value. |
| `RepoRoot` | `string` | Gets or sets the RepoRoot value. |
| `TimeoutMs` | `Nullable<int>` | Gets or sets the TimeoutMs value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
