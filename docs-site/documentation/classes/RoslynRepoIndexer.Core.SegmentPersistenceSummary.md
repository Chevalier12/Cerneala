# SegmentPersistenceSummary Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/SegmentedIndexCodec.cs`

Represents the structured output contract for SegmentPersistenceSummary operations.

```csharp
public sealed class SegmentPersistenceSummary
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `SegmentPersistenceSummary(int SegmentCount, int SegmentsWritten, int SegmentsReused, long SegmentBytes)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SegmentBytes` | `long` | Gets or sets the SegmentBytes value. |
| `SegmentCount` | `int` | Gets or sets the SegmentCount value. |
| `SegmentsReused` | `int` | Gets or sets the SegmentsReused value. |
| `SegmentsWritten` | `int` | Gets or sets the SegmentsWritten value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
