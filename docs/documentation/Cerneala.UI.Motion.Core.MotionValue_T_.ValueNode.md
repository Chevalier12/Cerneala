# MotionValue<T>.ValueNode Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionValue{T}.cs`

Adapts a `MotionValue<T>` instance to the `MotionNode` contract sampled by `MotionGraph`.

```csharp
private sealed class ValueNode : MotionNode
```

Containing type:
`MotionValue<T>`

Inheritance:
`object` -> `MotionNode` -> `MotionValue<T>.ValueNode`

## Examples

`ValueNode` is private to `MotionValue<T>`; callers create and animate the owning value through `MotionGraph`.

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

MotionGraph graph = new(new MotionThreadGuard(Environment.CurrentManagedThreadId));
MotionValue<double> opacity = graph.CreateValue(0d);

opacity.AnimateTo(1d, Motion.Tween<double>(TimeSpan.FromMilliseconds(100)));

MotionFrameResult result = graph.Tick(new MotionFrame(
    TimeSpan.FromMilliseconds(16),
    TimeSpan.FromMilliseconds(16),
    frameIndex: 1,
    MotionFrameReason.Manual,
    MotionFramePhase.BeforeRender));

int sampledNodes = result.MotionNodesSampled;
double current = opacity.Current;
```

## Remarks

`ValueNode` is created once by the `MotionValue<T>` constructor and kept in the owner's private `node` field. `MotionValue<T>.AnimateTo` registers this node with the owning graph while an animation is active. Completion, cancellation, disposal, or replacement of the active motion detaches the active sampler and unregisters the same node.

When `MotionGraph.Tick` samples the node, `ValueNode.Tick` calls the owner's internal `Advance(MotionFrame, out bool completed)` method. The returned `MotionNodeTickResult` reports the number of changed values from `Advance` through `ValuesChanged` and forwards the `completed` flag through `Completed`.

The node does not expose public API and does not implement registration callbacks. Its behavior is intentionally limited to bridging graph ticks to the owning value's sampler logic, including subscriber notification, velocity sampling, diagnostics recording, and completion handling performed by `MotionValue<T>`.

## Constructors

| Name | Description |
| --- | --- |
| `ValueNode(MotionValue<T> owner)` | Initializes the node with the `MotionValue<T>` instance to advance when the graph ticks. |

## Public Properties

| Name | Type | Description |
| --- | --- | --- |
| None |  | `ValueNode` exposes no public properties. |

## Public Methods

| Name | Return Type | Description |
| --- | --- | --- |
| None |  | `ValueNode` exposes no public methods. |

## Protected Internal Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Tick(MotionFrame frame)` | `MotionNodeTickResult` | Advances the owning `MotionValue<T>` for `frame` and reports value changes and completion to the graph. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `UI/Motion/Core/MotionValue{T}.cs`
- `UI/Motion/Core/MotionGraph.cs`
- `UI/Motion/Core/MotionNode.cs`
- `Cerneala.UI.Motion.Core.MotionValue<T>`
- `Cerneala.UI.Motion.Core.MotionNode`
