# MotionStartOptions Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionStartOptions.cs`

Configures how a `MotionValue<T>` starts or retargets an animation.

```csharp
public sealed record MotionStartOptions(
    RetargetMode RetargetMode = RetargetMode.Restart,
    MotionPriority Priority = MotionPriority.Normal,
    string? DebugName = null)
```

Inheritance:
`object` -> `MotionStartOptions`

## Examples
Start an animation that preserves elapsed progress when retargeting an active value:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

MotionValue<double> opacity = graph.CreateValue(0d);

opacity.AnimateTo(
    1d,
    MotionFactory.Tween<double>(TimeSpan.FromMilliseconds(160)),
    new MotionStartOptions(
        RetargetMode.PreserveProgress,
        MotionPriority.Normal,
        DebugName: "fade"));
```

## Remarks
`MotionStartOptions` is passed to `MotionValue<T>.AnimateTo`. Passing `null` uses `MotionStartOptions.Default`, which has `RetargetMode.Restart`, `MotionPriority.Normal`, and no debug name.

`RetargetMode.Restart` cancels the current active handle with `MotionCancelBehavior.KeepCurrent` and starts the new sampler from the current value with zero elapsed animation time.

`RetargetMode.PreserveProgress` is used only when the value already has an active sampler and active handle. In that path, the old handle is canceled, the new sampler is created from the current value to the new target, and the previous elapsed animation time is advanced into the new sampler. If the active motion cannot be detached safely, `MotionValue<T>` falls back to restart behavior while preserving the requested priority and debug name.

`DebugName` is passed into the motion graph's specification context when the sampler is created. `Priority` is stored on the options object; the current public priority enum exposes `MotionPriority.Normal`.

## Constructors
| Name | Description |
| --- | --- |
| `MotionStartOptions(RetargetMode RetargetMode = RetargetMode.Restart, MotionPriority Priority = MotionPriority.Normal, string? DebugName = null)` | Initializes a start-options record with retargeting, priority, and optional diagnostic naming settings. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `RetargetMode` | `RetargetMode` | Gets how `AnimateTo` handles an already active motion. The default is `RetargetMode.Restart`. |
| `Priority` | `MotionPriority` | Gets the motion priority value carried with the start request. The default is `MotionPriority.Normal`. |
| `DebugName` | `string?` | Gets the optional name supplied to the motion specification context when creating the sampler. |
| `Default` | `MotionStartOptions` | Gets the shared default options instance. |

## Applies to
Cerneala motion core values in the `Cerneala` project.

Target framework: `net8.0`

## See also
- `UI/Motion/Core/MotionStartOptions.cs`
- `UI/Motion/Core/MotionValue{T}.cs`
- `UI/Motion/Properties/MotionPropertyStartOptions.cs`
- `UI/Motion/Specs/RetargetMode.cs`
- `UI/Motion/Core/MotionPriority.cs`
