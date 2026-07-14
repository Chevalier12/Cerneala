# MotionFrame Struct

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionFrame.cs`

Represents the timestamp, elapsed delta, index, reason, and retained-frame phase used when sampling motion work.

```csharp
public readonly record struct MotionFrame(
    TimeSpan Now,
    TimeSpan Delta,
    int FrameIndex,
    MotionFrameReason Reason,
    MotionFramePhase Phase)
```

Inheritance:
`Object` -> `ValueType` -> `MotionFrame`

Implements:
`IEquatable<MotionFrame>`

## Examples

Create a manual frame and pass it to a motion graph tick:

```csharp
using Cerneala.UI.Motion.Core;

MotionGraph graph = new();

MotionFrame frame = new(
    Now: TimeSpan.FromMilliseconds(16),
    Delta: TimeSpan.FromMilliseconds(16),
    FrameIndex: 1,
    Reason: MotionFrameReason.Manual,
    Phase: MotionFramePhase.BeforeRender);

MotionFrameResult result = graph.Tick(frame);
```

Inspect the frame carried by a motion system tick result:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;

UIRoot root = new();
MotionFrameResult result = root.Motion.Tick(MotionFrameReason.Scheduled);

MotionFrame frame = result.Frame;
TimeSpan elapsed = frame.Delta;
MotionFramePhase phase = frame.Phase;
```

## Remarks

`MotionFrame` is the value passed through the motion sampling pipeline. `MotionSystem.Tick` creates frames from the configured motion clock and passes them to `MotionGraph.Tick`; `MotionFrameCoordinator` creates phase-specific frames while it moves through the retained UI frame lifecycle.

`Now` is the motion clock timestamp for the sample. `Delta` is the elapsed time used by motion specs for that sample. `MotionSystem.Tick` uses a zero delta for the first active tick after idle, clamps negative deltas to zero, and caps large deltas with `MotionSystem.MaxDelta`.

`FrameIndex` identifies active motion ticks from a `MotionSystem`. The index is incremented only when active motion or pending property writes are sampled. Empty phase results created by `MotionFrameCoordinator` use an index of `0`.

`Reason` describes why the frame was processed, such as scheduled, input, layout, or manual motion work. `Phase` records where the sample belongs in the retained frame pipeline.

Because `MotionFrame` is a `readonly record struct`, values are immutable after construction and use value-based equality. The compiler provides record-struct members such as deconstruction, equality, hashing, and string formatting based on the primary-constructor components.

## Constructors

| Name | Description |
| --- | --- |
| `MotionFrame(TimeSpan Now, TimeSpan Delta, int FrameIndex, MotionFrameReason Reason, MotionFramePhase Phase)` | Initializes a motion frame with the supplied timestamp, elapsed delta, frame index, reason, and phase. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Now` | `TimeSpan` | Gets the motion clock timestamp for the frame. |
| `Delta` | `TimeSpan` | Gets the elapsed time used to advance sampled motion for the frame. |
| `FrameIndex` | `int` | Gets the active motion tick index associated with the frame. |
| `Reason` | `MotionFrameReason` | Gets why the frame was requested. |
| `Phase` | `MotionFramePhase` | Gets the retained-frame phase associated with the frame. |

## Methods

| Name | Description |
| --- | --- |
| `Deconstruct(out TimeSpan Now, out TimeSpan Delta, out int FrameIndex, out MotionFrameReason Reason, out MotionFramePhase Phase)` | Deconstructs the frame into its primary-constructor components. |
| `Equals(MotionFrame other)` | Determines whether another frame has the same component values. |
| `GetHashCode()` | Returns a hash code based on the frame components. |
| `ToString()` | Returns the compiler-generated record string representation. |

## Applies to

Cerneala retained UI motion frame sampling.

## See also

- `Cerneala.UI.Motion.Core.MotionSystem`
- `Cerneala.UI.Motion.Core.MotionFrameCoordinator`
- `Cerneala.UI.Motion.Core.MotionFrameResult`
- `Cerneala.UI.Motion.Core.MotionFramePhase`
- `Cerneala.UI.Motion.Core.MotionFrameReason`
