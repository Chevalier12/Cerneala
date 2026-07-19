# PrismGraphBoundsStatus Enum

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraphOptimizer.cs`

Describes how safely a planned Prism node bound represents its pixel output.

```csharp
public enum PrismGraphBoundsStatus
```

## Examples

```csharp
using Cerneala.Drawing.Prism.Graph;

static bool CanAllocateStrictly(PrismGraphNodePlan node) =>
    node.BoundsStatus != PrismGraphBoundsStatus.Unknown;
```

## Remarks

`Unknown` is the default value and means `PrismGraphNodePlan.Bounds` must not be
used for strict clipping or allocation. `Conservative` is a guaranteed safe
superset of the output, while `Exact` identifies the exact known output
rectangle.

Bounds remain in transformed logical drawing coordinates. Convert them to
surface pixels with the owning `PrismGraphScope.PixelScale`; do not apply the
scope's effective transform a second time.

## Fields

| Name | Description |
| --- | --- |
| `Unknown` | The available rectangle is not safe for strict clipping or allocation. |
| `Exact` | The rectangle exactly describes the known pixel output. |
| `Conservative` | The rectangle safely contains the output and may overestimate it. |

## Applies to

Cerneala retained Prism graph bounds planning.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismGraphNodePlan`
- `Cerneala.Drawing.Prism.Graph.PrismGraphScope`
- `Cerneala.Drawing.Prism.Graph.PrismGraphOptimizer`
