# MotionNodeTickResult Struct

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionNode.cs`

Reports the per-frame work performed by a `MotionNode` during a `MotionGraph` tick.

```csharp
public readonly record struct MotionNodeTickResult(
    int ValuesChanged = 0,
    int PropertyWrites = 0,
    bool Completed = false,
    int RenderInvalidations = 0,
    int LayoutInvalidations = 0,
    int SkippedByReducedMotion = 0)
```

Inheritance:
`Object` -> `ValueType` -> `MotionNodeTickResult`

Implements:
`IEquatable<MotionNodeTickResult>`

## Examples

Return a result from a custom motion node that changes one value and completes immediately:

```csharp
using Cerneala.UI.Motion.Core;

sealed class OneShotNode : MotionNode
{
    protected internal override MotionNodeTickResult Tick(MotionFrame frame)
    {
        return new MotionNodeTickResult(ValuesChanged: 1, Completed: true);
    }
}
```

Return the default result when a node stays active but performs no counted work:

```csharp
using Cerneala.UI.Motion.Core;

MotionNodeTickResult result = new();
```

## Remarks

`MotionNodeTickResult` is returned by `MotionNode.Tick(MotionFrame)`. `MotionGraph.Tick` adds the counters from each sampled node into the resulting `MotionFrameResult`.

When `Completed` is `true`, `MotionGraph` unregisters the node after the tick result is recorded. A result with the default values reports no changes, no invalidations, no reduced-motion skips, and leaves the node active.

The record struct is immutable after construction and uses value-based equality. The compiler provides deconstruction, equality, hashing, and string formatting based on the primary-constructor components.

## Constructors

| Name | Description |
| --- | --- |
| `MotionNodeTickResult(int ValuesChanged = 0, int PropertyWrites = 0, bool Completed = false, int RenderInvalidations = 0, int LayoutInvalidations = 0, int SkippedByReducedMotion = 0)` | Initializes a tick result with counters for the work performed by one node. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ValuesChanged` | `int` | Gets the number of motion values changed by the node during the tick. |
| `PropertyWrites` | `int` | Gets the number of motion property writes reported by the node during the tick. |
| `Completed` | `bool` | Gets whether the node completed and should be unregistered by the graph. |
| `RenderInvalidations` | `int` | Gets the number of render invalidations reported by the node during the tick. |
| `LayoutInvalidations` | `int` | Gets the number of layout invalidations reported by the node during the tick. |
| `SkippedByReducedMotion` | `int` | Gets the number of motion operations skipped because of reduced-motion handling. |

## Methods

| Name | Description |
| --- | --- |
| `Deconstruct(out int ValuesChanged, out int PropertyWrites, out bool Completed, out int RenderInvalidations, out int LayoutInvalidations, out int SkippedByReducedMotion)` | Deconstructs the result into its primary-constructor components. |
| `Equals(MotionNodeTickResult other)` | Determines whether another result has the same component values. |
| `GetHashCode()` | Returns a hash code based on the result components. |
| `ToString()` | Returns the compiler-generated record string representation. |

## Applies to

Cerneala retained UI motion graph sampling.

## See also

- `Cerneala.UI.Motion.Core.MotionNode`
- `Cerneala.UI.Motion.Core.MotionGraph`
- `Cerneala.UI.Motion.Core.MotionFrame`
- `Cerneala.UI.Motion.Core.MotionFrameResult`
