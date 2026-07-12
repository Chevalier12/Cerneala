# IndexManifest Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the public IndexManifest contract used by Roslyn Repo Indexer.

```csharp
public sealed class IndexManifest
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `IndexManifest()` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ConfigHash` | `string` | Gets or sets the ConfigHash value. |
| `CreatedUtc` | `DateTimeOffset` | Gets or sets the CreatedUtc value. |
| `DiscoveryFingerprint` | `string` | Gets or sets the DiscoveryFingerprint value. |
| `DocumentCount` | `int` | Gets or sets the DocumentCount value. |
| `DocumentsByRelativePath` | `IReadOnlyDictionary<string, DocumentState>` | Gets or sets the DocumentsByRelativePath value. |
| `GenerationId` | `string` | Gets or sets the GenerationId value. |
| `RecentWarnings` | `IReadOnlyList<string>` | Gets or sets the RecentWarnings value. |
| `ReferenceCount` | `int` | Gets or sets the ReferenceCount value. |
| `RepoRoot` | `string` | Gets or sets the RepoRoot value. |
| `RepositoryStateFingerprint` | `string` | Gets or sets the RepositoryStateFingerprint value. |
| `SchemaVersion` | `int` | Gets or sets the SchemaVersion value. |
| `SegmentBytes` | `long` | Gets or sets the SegmentBytes value. |
| `SegmentCount` | `int` | Gets or sets the SegmentCount value. |
| `SegmentsReused` | `int` | Gets or sets the SegmentsReused value. |
| `SegmentsWritten` | `int` | Gets or sets the SegmentsWritten value. |
| `StorageFormat` | `string` | Gets or sets the StorageFormat value. |
| `SymbolCount` | `int` | Gets or sets the SymbolCount value. |
| `Timings` | `IndexTimingSummary` | Gets or sets the Timings value. |
| `TokenCount` | `int` | Gets or sets the TokenCount value. |
| `ToolVersion` | `string` | Gets or sets the ToolVersion value. |
| `UpdatedUtc` | `DateTimeOffset` | Gets or sets the UpdatedUtc value. |
| `WarningCount` | `int` | Gets or sets the WarningCount value. |
| `WorkspaceInputs` | `IReadOnlyList<WorkspaceInput>` | Gets or sets the WorkspaceInputs value. |
| `WorkspaceInputsHash` | `string` | Gets or sets the WorkspaceInputsHash value. |

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `CreateNew(string repoRoot, string configHash, string workspaceInputsHash)` | `IndexManifest` | Executes the `CreateNew` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
