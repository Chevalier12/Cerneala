# MotionFrameCoordinator Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionFrameCoordinator.cs`

Coordinates motion sampling and motion diagnostics across the retained UI frame phases.

```csharp
public sealed class MotionFrameCoordinator
```

Inheritance:
`object` -> `MotionFrameCoordinator`

## Examples

Advance the motion coordinator through a manual frame:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;

UIRoot root = new();

MotionFrameResult begin = root.Motion.Frames.BeginFrame(MotionFrameReason.Manual);
MotionFrameResult beforeLayout = root.Motion.Frames.BeforeLayout();
MotionFrameResult afterLayout = root.Motion.Frames.AfterLayout();
MotionFrameResult beforeRender = root.Motion.Frames.BeforeRender();
MotionFrameResult end = root.Motion.Frames.EndFrame();
```

Use the root frame pipeline so the scheduler invokes the motion coordinator around layout and render:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Motion.Core;

UIRoot root = new();
FrameStats stats = root.ProcessFrame(motionReason: MotionFrameReason.Scheduled);
```

## Remarks

`MotionFrameCoordinator` is created by `MotionSystem` and exposed through `MotionSystem.Frames`. In normal use, `UIRoot.ProcessFrame` passes it to `UiFrameScheduler.ProcessFrame`, which calls the coordinator before layout, after layout, before render, and after render.

Every public method verifies motion thread affinity through `MotionSystem.ThreadGuard`. Calls must run on the thread that created the owning `MotionSystem`.

`BeginFrame` starts diagnostic frame tracking, stores the frame reason for later phases, resets the per-frame sampled flag, and records `MotionFramePhase.AfterInput` for input-driven frames or `MotionFramePhase.PreInput` for all other reasons.

`BeforeLayout` records the layout phase, captures motion diagnostics and the first layout-motion snapshots, and samples active motion through `MotionSystem.Tick` when motion is active. `AfterLayout` records the phase, captures after-layout diagnostics, and starts layout-motion correction work from the captured layout snapshots.

`BeforeRender` records the render phase and samples motion only if it was not already sampled in `BeforeLayout`. This keeps a frame from ticking motion twice while still allowing render-only motion work to advance when no before-layout sample happened. `EndFrame` records `MotionFramePhase.AfterRender`.

Methods that do not sample active motion return an empty `MotionFrameResult` with a phase-specific `MotionFrame`. The coordinator itself does not process invalidation queues; that remains the scheduler's job.

## Constructors

| Name | Description |
| --- | --- |
| `MotionFrameCoordinator(UIRoot, MotionSystem)` | Initializes a coordinator for the supplied root and motion system. Throws `ArgumentNullException` when `root` or `motion` is `null`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `BeginFrame(MotionFrameReason reason)` | `MotionFrameResult` | Begins a motion-aware frame, records the pre-input or after-input phase, and returns an empty result for that phase. |
| `BeforeLayout()` | `MotionFrameResult` | Records the before-layout phase, captures first layout-motion snapshots, and samples active motion when present. |
| `AfterLayout()` | `MotionFrameResult` | Records the after-layout phase, captures last layout-motion snapshots, starts layout corrections, and returns an empty result for the phase. |
| `BeforeRender()` | `MotionFrameResult` | Records the before-render phase and samples active motion only when the frame has not already sampled before layout. |
| `EndFrame()` | `MotionFrameResult` | Records the after-render phase and returns an empty result for the phase. |

## Method Details

### BeginFrame

```csharp
public MotionFrameResult BeginFrame(MotionFrameReason reason)
```

Records `MotionFramePhase.AfterInput` when `reason` is `MotionFrameReason.Input`; otherwise records `MotionFramePhase.PreInput`.

### BeforeLayout

```csharp
public MotionFrameResult BeforeLayout()
```

Captures diagnostics before layout and calls `MotionSystem.Tick` at `MotionFramePhase.BeforeLayout` when `MotionSystem.HasActiveMotion` is `true`.

### AfterLayout

```csharp
public MotionFrameResult AfterLayout()
```

Captures diagnostics after layout and asks the layout-motion coordinator to capture final snapshots and start correction animations.

### BeforeRender

```csharp
public MotionFrameResult BeforeRender()
```

Calls `MotionSystem.Tick` at `MotionFramePhase.BeforeRender` only when the current frame has not already sampled motion.

### EndFrame

```csharp
public MotionFrameResult EndFrame()
```

Records `MotionFramePhase.AfterRender`.

## Applies to

Cerneala retained UI motion frame processing.

## See also

- `Cerneala.UI.Motion.Core.MotionSystem`
- `Cerneala.UI.Motion.Core.MotionFrame`
- `Cerneala.UI.Motion.Core.MotionFrameResult`
- `Cerneala.UI.Motion.Core.MotionFramePhase`
- `Cerneala.UI.Invalidation.UiFrameScheduler`
- `Cerneala.UI.Elements.UIRoot`
