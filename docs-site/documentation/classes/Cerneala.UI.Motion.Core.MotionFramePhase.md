# MotionFramePhase Enum

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionFramePhase.cs`

Names a point in a UI frame where the motion system runs.

```csharp
public enum MotionFramePhase
```

## Examples

Read the phases recorded during a frame that has an active animation:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

UIRoot root = new(100, 100);
MotionValue<float> value = root.Motion.Graph.CreateValue(0f);
value.AnimateTo(10f, Motion.Tween<float>(TimeSpan.FromSeconds(1)));

root.ProcessFrame();
Console.WriteLine(string.Join(", ", root.Detective.Motion.Phases));
// PreInput, BeforeLayout, AfterLayout, BeforeRender, AfterRender
```

## Remarks

`MotionFrameCoordinator` (reached through `UIRoot.Motion.Frames`) records one phase per call in `MotionDiagnostics.Phases`, which is cleared when a new frame begins:

| Coordinator call | Phase recorded | What else happens |
| --- | --- | --- |
| `BeginFrame(reason)` | `AfterInput` when `reason` is `MotionFrameReason.Input`, otherwise `PreInput` | No sampling. |
| `BeforeLayout()` | `BeforeLayout` | Samples motion when motion is active. |
| `AfterLayout()` | `AfterLayout` | Starts layout corrections. No sampling. |
| `BeforeRender()` | `BeforeRender` | Samples motion only when `BeforeLayout` did not sample in this frame. |
| `EndFrame()` | `AfterRender` | No sampling. |

So motion is sampled at most once per frame: at `BeforeLayout` when motion is active then, otherwise at `BeforeRender`.

The phase is also stored in `MotionFrame.Phase` and in the `MotionFrame` returned through `MotionFrameResult.Frame`. `MotionSystem.Tick` takes a phase parameter that defaults to `BeforeRender` and copies it into the frame. Motion nodes receive the phase but the framework does not change sampling based on it.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `PreInput` | `0` | Start of a frame that was not caused by input. |
| `AfterInput` | `1` | Start of a frame caused by input. |
| `BeforeLayout` | `2` | Before layout. First point where motion is sampled. |
| `AfterLayout` | `3` | After layout. Layout corrections start here. |
| `BeforeRender` | `4` | Before rendering. Motion is sampled here if it was not sampled before layout. The default phase of `MotionSystem.Tick`. |
| `AfterRender` | `5` | End of the frame. |

## Applies to

Cerneala motion frame scheduling.

## See also

- `Cerneala.UI.Motion.Core.MotionFrameCoordinator`
- `Cerneala.UI.Motion.Core.MotionFrame`
- `Cerneala.UI.Motion.Core.MotionFrameReason`
- `Cerneala.UI.Motion.Core.MotionSystem`
- `Cerneala.UI.Detective.MotionDiagnostics`
