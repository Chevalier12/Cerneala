# IndexStore Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/IndexStore.cs`

Represents the public IndexStore contract used by Roslyn Repo Indexer.

```csharp
public sealed class IndexStore
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `Clean(string repoRoot)` | `void` | Executes the `Clean` operation. |
| `Exists(string repoRoot)` | `bool` | Executes the `Exists` operation. |
| `GetCurrentPointerPath(string repoRoot)` | `string` | Executes the `GetCurrentPointerPath` operation. |
| `GetExactReferenceCacheDirectory(string repoRoot)` | `string` | Executes the `GetExactReferenceCacheDirectory` operation. |
| `GetGenerationDirectory(string repoRoot, string generationId)` | `string` | Executes the `GetGenerationDirectory` operation. |
| `GetGenerationsDirectory(string repoRoot)` | `string` | Executes the `GetGenerationsDirectory` operation. |
| `GetGenerationStamp(string repoRoot)` | `IndexGenerationStamp` | Executes the `GetGenerationStamp` operation. |
| `GetIndexDirectory(string repoRoot)` | `string` | Executes the `GetIndexDirectory` operation. |
| `GetManifestPath(string repoRoot)` | `string` | Executes the `GetManifestPath` operation. |
| `GetStatus(string repoRoot)` | `StatusSummary` | Executes the `GetStatus` operation. |
| `GetVersionDirectory(string repoRoot)` | `string` | Executes the `GetVersionDirectory` operation. |
| `Read(string repoRoot)` | `IndexSnapshot` | Executes the `Read` operation. |
| `ReadGeneration(string repoRoot, string generationId)` | `IndexSnapshot` | Executes the `ReadGeneration` operation. |
| `ReadManifest(string repoRoot)` | `IndexManifest` | Executes the `ReadManifest` operation. |
| `ReadPrevious(string repoRoot)` | `IndexSnapshot` | Executes the `ReadPrevious` operation. |
| `TryReadExactReferenceCache(string repoRoot, string cacheKey, DateTimeOffset indexUpdatedUtc, IReadOnlyList`1& results)` | `bool` | Executes the `TryReadExactReferenceCache` operation. |
| `ValidateGenerationFiles(string repoRoot, IndexManifest manifest)` | `void` | Executes the `ValidateGenerationFiles` operation. |
| `Write(string repoRoot, IndexSnapshot snapshot)` | `IndexTimingSummary` | Executes the `Write` operation. |
| `WriteExactReferenceCache(string repoRoot, string cacheKey, DateTimeOffset indexUpdatedUtc, IReadOnlyList<SearchResult> results)` | `void` | Executes the `WriteExactReferenceCache` operation. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
