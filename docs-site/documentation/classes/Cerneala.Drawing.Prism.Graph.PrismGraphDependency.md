# PrismGraphDependency Struct

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraph.cs`

Stores one typed, keyed, and versioned dependency of a Prism graph node.

```csharp
public readonly record struct PrismGraphDependency(
    PrismGraphDependencyKind Kind,
    long Key,
    long Version);
```

## Examples

```csharp
using Cerneala.Drawing.Prism.Graph;

PrismGraphDependency dependency = new(
    PrismGraphDependencyKind.VisualContent,
    Key: 42,
    Version: 7);
```

## Remarks

`Kind` describes the input domain, `Key` identifies the input within that domain, and `Version` is the captured value used for invalidation or cache comparison. The builder adds scope-wide dependencies to every node and operation-specific dependencies where needed.

## Constructors

| Name | Description |
| --- | --- |
| `PrismGraphDependency(PrismGraphDependencyKind kind, long key, long version)` | Creates an immutable dependency snapshot. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Kind` | `PrismGraphDependencyKind` | Gets the dependency domain. |
| `Key` | `long` | Gets the stable key within the domain. |
| `Version` | `long` | Gets the captured version or stable input value. |

## Applies to

Cerneala retained Prism graph invalidation and caching.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismGraphDependencyKind`
- `Cerneala.Drawing.Prism.Graph.PrismGraphNode`
- `Cerneala.Drawing.Prism.Graph.PrismDependencyStamp`
