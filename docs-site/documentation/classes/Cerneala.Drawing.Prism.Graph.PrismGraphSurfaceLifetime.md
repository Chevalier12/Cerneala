# PrismGraphSurfaceLifetime Struct

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraphOptimizer.cs`

Describes the inclusive execution interval during which one planned Prism
surface must remain live.

```csharp
public readonly record struct PrismGraphSurfaceLifetime
```

## Examples

```csharp
using Cerneala.Drawing.Prism.Graph;

static bool IsLive(PrismGraphSurfaceLifetime lifetime, int step) =>
    lifetime.FirstStep <= step && step <= lifetime.LastStep;
```

## Remarks

Instances are produced by `PrismGraphOptimizer`; the constructor is not public.
`FirstStep` and `LastStep` are inclusive indices into
`PrismGraphExecutionPlan.ExecutionOrder`.

A control capture belonging to an outer scope can have a `FirstStep` earlier
than its own node's execution index because that capture must remain available
while nested scopes execute. The intervals are planning metadata only and do
not allocate or own GPU surfaces.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `NodeId` | `PrismGraphNodeId` | Gets the node whose planned surface is tracked. |
| `FirstStep` | `int` | Gets the first inclusive execution step at which the surface is live. |
| `LastStep` | `int` | Gets the last inclusive execution step at which the surface is live. |

## Applies to

Cerneala retained Prism surface reuse and peak planning.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismGraphExecutionPlan`
- `Cerneala.Drawing.Prism.Graph.PrismGraphOptimizer`
- `Cerneala.Drawing.Prism.Graph.PrismGraphNodeId`
