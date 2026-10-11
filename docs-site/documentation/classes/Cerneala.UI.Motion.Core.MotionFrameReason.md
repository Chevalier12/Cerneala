# MotionFrameReason Enum

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionFrame.cs`

Records why a motion frame ran.

```csharp
public enum MotionFrameReason
```

## Examples

Tick the motion system with an explicit reason and read it back from the frame:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;

UIRoot root = new(100, 100);
MotionFrameResult result = root.Motion.Tick(MotionFrameReason.Manual);
Console.WriteLine(result.Frame.Reason); // Manual
```

## Remarks

The reason is stored in `MotionFrame.Reason`. Most framework paths use `Scheduled`: it is the default of `UIRoot.ProcessFrame(motionReason)`, `UiFrameScheduler.ProcessFrame`, and `MotionSystem.Tick(reason)`.

Only `Input` changes behavior. `MotionFrameCoordinator.BeginFrame(MotionFrameReason.Input)` records the `MotionFramePhase.AfterInput` phase instead of `PreInput`. `UiHost` processes input frames with `Input`; when such a frame has active motion, motion is sampled with reason `Input`.

`Initial`, `Layout`, and `Manual` are not produced by the framework. Code that drives `MotionSystem.Tick` or `MotionGraph.Tick` directly can pass them to label its frames.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Initial` | `0` | Label for a first frame. Not produced by the framework. |
| `Scheduled` | `1` | A regular scheduled frame. The default reason. |
| `Input` | `2` | A frame processed after input. Makes `BeginFrame` record `AfterInput`. |
| `Layout` | `3` | Label for a layout-driven frame. Not produced by the framework. |
| `Manual` | `4` | Label for a frame driven by application or test code. Not produced by the framework. |

## Applies to

Cerneala motion frame scheduling.

## See also

- `Cerneala.UI.Motion.Core.MotionFrame`
- `Cerneala.UI.Motion.Core.MotionFramePhase`
- `Cerneala.UI.Motion.Core.MotionFrameCoordinator`
- `Cerneala.UI.Motion.Core.MotionSystem`
