# MotionNode Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionNode.cs`

Defines the base class for schedulable motion work advanced by a `MotionGraph`.

```csharp
public abstract class MotionNode
```

Inheritance:
`object` -> `MotionNode`

## Examples

Create a node that completes after one graph tick:

```csharp
using Cerneala.UI.Motion.Core;

MotionGraph graph = new();
OneShotNode node = new();

graph.Register(node);

MotionFrame frame = new(
    TimeSpan.FromMilliseconds(16),
    TimeSpan.FromMilliseconds(16),
    frameIndex: 1,
    MotionFrameReason.Manual,
    MotionFramePhase.BeforeRender);

MotionFrameResult result = graph.Tick(frame);

sealed class OneShotNode : MotionNode
{
    protected internal override MotionNodeTickResult Tick(MotionFrame frame)
    {
        return new MotionNodeTickResult(ValuesChanged: 1, Completed: true);
    }
}
```

## Remarks

`MotionNode` is the low-level unit sampled by `MotionGraph`. Derived nodes implement `Tick(MotionFrame)` to perform per-frame motion work and return a `MotionNodeTickResult` that reports changed values, property writes, invalidations, reduced-motion skips, and whether the node has completed.

`MotionGraph.Register` marks a node as registered and calls `OnRegistered(MotionGraph)`. `MotionGraph.Unregister` clears the registration flag and calls `OnUnregistered()`. The graph also unregisters a node automatically when its tick result reports `Completed` as `true`.

Registration changes are safe during a graph tick. If a node registers or unregisters while the graph is sampling, the graph queues the change and applies it after the current sampling pass. The node's internal registration flag prevents duplicate registrations.

Concrete node types in the motion system include private implementations used by `MotionValue<T>` and `MotionPropertyBinding<T>`.

## Constructors

| Name | Description |
| --- | --- |
| `MotionNode()` | Initializes a derived motion node instance. |

## Public Properties

| Name | Type | Description |
| --- | --- | --- |
| None |  | `MotionNode` exposes no public properties. |

## Public Methods

| Name | Return Type | Description |
| --- | --- | --- |
| None |  | `MotionNode` exposes no public methods. |

## Protected Internal Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Tick(MotionFrame frame)` | `MotionNodeTickResult` | Advances the node for the supplied frame and reports the work performed. Derived classes must implement this method. |
| `OnRegistered(MotionGraph graph)` | `void` | Called after the graph registers the node. The base implementation does nothing. |
| `OnUnregistered()` | `void` | Called after the graph unregisters the node. The base implementation does nothing. |

## Internal Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsRegistered` | `bool` | Tracks whether the node is currently registered with a graph. The graph uses this flag to avoid duplicate registrations and no-op removals. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `UI/Motion/Core/MotionNode.cs`
- `UI/Motion/Core/MotionGraph.cs`
- `UI/Motion/Core/MotionNodeTickResult`
- `UI/Motion/Core/MotionValue{T}.cs`
- `UI/Motion/Properties/MotionPropertyBinding{T}.cs`
