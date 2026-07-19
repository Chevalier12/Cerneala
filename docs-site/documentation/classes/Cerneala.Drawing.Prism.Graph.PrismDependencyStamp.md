# PrismDependencyStamp Struct

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismDependencyStamp.cs`

Captures the retained and runtime versions that determine one Prism scope's composition dependencies.

```csharp
public readonly record struct PrismDependencyStamp(
    PrismCacheOwnerToken CacheOwnerToken,
    PrismStructuralVersion StructuralVersion,
    PrismValueVersion ValueVersion,
    long VisualContentVersion,
    long DescendantVersion);
```

## Examples

```csharp
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Runtime;

PrismDependencyStamp stamp = new(
    new PrismCacheOwnerToken(42),
    new PrismStructuralVersion(3),
    new PrismValueVersion(9),
    VisualContentVersion: 12,
    DescendantVersion: 0);
```

## Remarks

The stamp is immutable and has record value equality. `PrismFrameAnalyzer` snapshots the current scope versions and folds nested scope stamps into `DescendantVersion`, allowing an outer scope dependency to change when a nested Prism value changes without rebuilding the command list.

`DescendantVersion` is an opaque aggregate. Consumers should compare it for equality rather than interpret its numeric value.

## Constructors

| Name | Description |
| --- | --- |
| `PrismDependencyStamp(PrismCacheOwnerToken, PrismStructuralVersion, PrismValueVersion, long, long)` | Creates a dependency stamp from the cache owner, current Prism versions, retained visual version, and nested dependency aggregate. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `CacheOwnerToken` | `PrismCacheOwnerToken` | Gets the retained cache identity for the scope owner. |
| `StructuralVersion` | `PrismStructuralVersion` | Gets the Prism topology version captured during analysis. |
| `ValueVersion` | `PrismValueVersion` | Gets the Prism runtime value version captured during analysis. |
| `VisualContentVersion` | `long` | Gets the retained visual content generation captured for the scope. |
| `DescendantVersion` | `long` | Gets the opaque aggregate of nested Prism dependencies. |

## Applies to

Cerneala Prism retained composition invalidation and cache keys.

## See also

- `Cerneala.Drawing.Prism.PrismCacheOwnerToken`
- `Cerneala.Drawing.Prism.Graph.PrismAnalyzedScope`
- `Cerneala.Drawing.Prism.Graph.PrismFrameAnalysis`
