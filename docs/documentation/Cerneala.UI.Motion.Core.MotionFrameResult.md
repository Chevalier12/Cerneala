# MotionFrameResult Struct

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionFrameResult.cs`

Represents the aggregate counters and continuation signal produced by a motion graph or motion system tick.

```csharp
public readonly record struct MotionFrameResult(
    MotionFrame Frame,
    bool NeedsAnotherFrame,
    int MotionFrames,
    int MotionNodesSampled,
    int MotionValuesChanged,
    int MotionPropertyWrites,
    int MotionCompleted,
    int MotionRenderInvalidations,
    int MotionLayoutInvalidations,
    int MotionSkippedByReducedMotion)
```

Inheritance:
`Object` -> `ValueType` -> `MotionFrameResult`

Implements:
`IEquatable<MotionFrameResult>`

## Examples

Tick a root-owned motion system and inspect whether it did work:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;

UIRoot root = new();

MotionFrameResult result = root.Motion.Tick(MotionFrameReason.Manual);
if (result.HasWork || result.NeedsAnotherFrame)
{
    MotionFrame frame = result.Frame;
    int sampledNodes = result.MotionNodesSampled;
}
```

Create an empty result for a manually supplied frame:

```csharp
using Cerneala.UI.Motion.Core;

MotionFrame frame = new(
    TimeSpan.Zero,
    TimeSpan.Zero,
    frameIndex: 0,
    MotionFrameReason.Manual,
    MotionFramePhase.BeforeRender);

MotionFrameResult result = MotionFrameResult.Empty(frame);
```

## Remarks

`MotionFrameResult` is returned by `MotionGraph.Tick`, `MotionSystem.Tick`, and phase-oriented motion frame coordination. It carries the sampled `MotionFrame`, a `NeedsAnotherFrame` flag, and the counters produced while sampling motion nodes and flushing motion-backed property writes.

`MotionGraph.Tick` returns `MotionFrameResult.Empty(frame)` when the graph has no active nodes. Otherwise it reports one motion frame, the number of sampled nodes, value changes, completed nodes, property writes, render invalidations, layout invalidations, and reduced-motion skips gathered from the active nodes.

`MotionSystem.Tick` combines graph counters with pending `MotionPropertyStore` flush counters. When the system is idle, the empty result has `MotionFrames` set to `0`, `NeedsAnotherFrame` set to `false`, and `HasWork` set to `false`.

`HasWork` is derived from the numeric work counters only. It does not consider `Frame` or `NeedsAnotherFrame`; a result can request another frame because motion remains active, while `HasWork` describes whether this specific result reported counted work.

Because `MotionFrameResult` is a `readonly record struct`, values are immutable after construction and use value-based equality. The compiler provides record-struct members such as deconstruction, equality, hashing, and string formatting based on the primary-constructor components.

## Constructors

| Name | Description |
| --- | --- |
| `MotionFrameResult(MotionFrame Frame, bool NeedsAnotherFrame, int MotionFrames, int MotionNodesSampled, int MotionValuesChanged, int MotionPropertyWrites, int MotionCompleted, int MotionRenderInvalidations, int MotionLayoutInvalidations, int MotionSkippedByReducedMotion)` | Initializes a motion frame result with the supplied frame, continuation flag, and motion counters. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Frame` | `MotionFrame` | Gets the motion frame associated with the result. |
| `NeedsAnotherFrame` | `bool` | Gets whether active motion or pending motion property writes require another frame. |
| `MotionFrames` | `int` | Gets the number of motion frames counted by this result. Empty results use `0`; sampled graph or system ticks use `1`. |
| `MotionNodesSampled` | `int` | Gets the number of motion nodes sampled during the tick. |
| `MotionValuesChanged` | `int` | Gets the number of motion value changes reported during the tick. |
| `MotionPropertyWrites` | `int` | Gets the number of motion-backed property writes performed by node sampling or property flushing. |
| `MotionCompleted` | `int` | Gets the number of motion nodes that completed during the tick. |
| `MotionRenderInvalidations` | `int` | Gets the number of render invalidations caused by motion work. |
| `MotionLayoutInvalidations` | `int` | Gets the number of layout invalidations caused by motion work. |
| `MotionSkippedByReducedMotion` | `int` | Gets the number of motion operations skipped because of reduced-motion policy. |
| `HasWork` | `bool` | Gets whether any numeric motion work counter is greater than zero. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Empty(MotionFrame frame)` | `MotionFrameResult` | Creates an empty result for `frame` with `NeedsAnotherFrame` set to `false` and all numeric counters set to `0`. |
| `Deconstruct(out MotionFrame Frame, out bool NeedsAnotherFrame, out int MotionFrames, out int MotionNodesSampled, out int MotionValuesChanged, out int MotionPropertyWrites, out int MotionCompleted, out int MotionRenderInvalidations, out int MotionLayoutInvalidations, out int MotionSkippedByReducedMotion)` | `void` | Deconstructs the result into its primary-constructor components. |
| `Equals(MotionFrameResult other)` | `bool` | Determines whether another result has the same component values. |
| `GetHashCode()` | `int` | Returns a hash code based on the result components. |
| `ToString()` | `string` | Returns the compiler-generated record string representation. |

## Method Details

### Empty

```csharp
public static MotionFrameResult Empty(MotionFrame frame)
```

Returns a result that preserves the supplied `MotionFrame`, sets `NeedsAnotherFrame` to `false`, and sets every numeric counter to `0`.

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Core.MotionFrame`
- `Cerneala.UI.Motion.Core.MotionGraph`
- `Cerneala.UI.Motion.Core.MotionSystem`
- `Cerneala.UI.Motion.Core.MotionFrameCoordinator`
- `Cerneala.UI.Invalidation.FrameStats`
