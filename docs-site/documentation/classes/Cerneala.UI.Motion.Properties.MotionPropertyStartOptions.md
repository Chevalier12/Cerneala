# MotionPropertyStartOptions Class

## Definition

Namespace: `Cerneala.UI.Motion.Properties`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Properties/MotionPropertyStartOptions.cs`

Configures how a motion property animation starts, retargets, participates in diagnostics, and clears or holds its final property value.

```csharp
public sealed class MotionPropertyStartOptions
```

Inheritance:
`object` -> `MotionPropertyStartOptions`

## Examples

Start a property animation with the default start behavior:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;
using Cerneala.UI.Media;

UIRoot root = new();
Control control = new();
root.VisualChildren.Add(control);

MotionValue<Brush?> value =
    root.Motion.Graph.CreateValue(control.Background);

using MotionPropertyBinding<Brush?> binding =
    new(root.Motion, control, Control.BackgroundProperty, value);

MotionHandle handle = binding.AnimateTo(
    new SolidColorBrush(Color.White),
    Motion.Tween<Brush?>(TimeSpan.FromMilliseconds(100)),
    MotionPropertyStartOptions.Default);
```

Hold the completed animated value and pass a debug name to the underlying motion spec context:

```csharp
binding.AnimateTo(
    new SolidColorBrush(Color.White),
    Motion.Tween<Brush?>(TimeSpan.FromMilliseconds(100)),
    new MotionPropertyStartOptions
    {
        RetargetMode = RetargetMode.PreserveProgress,
        Priority = MotionPriority.Normal,
        DebugName = "Background fade",
        HoldOnComplete = true
    });
```

## Remarks

`MotionPropertyStartOptions` is the property-binding counterpart to `MotionStartOptions`. `MotionPropertyBinding<T>.AnimateTo` uses it to start the underlying `MotionValue<T>` animation and to decide what happens to the animated UI property when the animation completes.

`RetargetMode`, `Priority`, and `DebugName` are forwarded to the core motion layer. `RetargetMode.PreserveProgress` allows an already active `MotionValue<T>` animation to reuse its elapsed progress when retargeting is possible; otherwise the core motion value falls back to restart behavior. `DebugName` flows into `MotionSpecContext` and diagnostic traces that record motion-related events.

`HoldOnComplete` is handled by `MotionPropertyBinding<T>` rather than by `MotionValue<T>`. When `false`, the binding clears the animation value source after natural completion so the target property falls back to its next available source. When `true`, the binding stages the current animated value after completion so the property keeps the final animation value.

The type is mutable only through init-only properties. Use object initializer syntax for per-animation options, or `Default` when the default behavior is enough.

## Constructors

| Name | Description |
| --- | --- |
| `MotionPropertyStartOptions()` | Initializes options with `RetargetMode.Restart`, `MotionPriority.Normal`, `null` debug name, and `HoldOnComplete` set to `false`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Default` | `MotionPropertyStartOptions` | Gets the shared default options instance. |
| `RetargetMode` | `RetargetMode` | Gets the core retargeting mode forwarded to `MotionValue<T>.AnimateTo`; the default is `RetargetMode.Restart`. |
| `Priority` | `MotionPriority` | Gets the motion priority forwarded to the core motion start options; the default is `MotionPriority.Normal`. |
| `DebugName` | `string?` | Gets the optional diagnostic name forwarded to the core motion spec context. |
| `HoldOnComplete` | `bool` | Gets whether `MotionPropertyBinding<T>` should keep the current animated property value after natural completion instead of clearing the animation source. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Properties.MotionPropertyBinding<T>`
- `Cerneala.UI.Motion.Core.MotionStartOptions`
- `Cerneala.UI.Motion.Core.MotionValue<T>`
- `Cerneala.UI.Motion.Core.MotionHandle`
- `Cerneala.UI.Motion.Specs.RetargetMode`
- `Cerneala.UI.Motion.Core.MotionPriority`
