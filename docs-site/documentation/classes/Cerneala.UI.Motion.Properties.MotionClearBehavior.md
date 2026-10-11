# MotionClearBehavior Enum

## Definition
Namespace: `Cerneala.UI.Motion.Properties`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Properties/MotionClearBehavior.cs`

Specifies what a property shows after its motion binding is cleared.

```csharp
public enum MotionClearBehavior
```

## Examples

Clear an opacity animation halfway and keep the value it reached:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;

ManualClock clock = new();
UIRoot root = new(100, 100, motionClock: clock);
UIElement element = new();
root.VisualChildren.Add(element);
root.ProcessFrame();

MotionPropertyBinding<float> binding =
    root.Motion.Properties.GetOrCreateBinding(root.Motion, element, UIElement.OpacityProperty);
binding.AnimateTo(0f, new TweenSpec<float>(TimeSpan.FromMilliseconds(100), Easings.Linear));

for (int ms = 0; ms <= 48; ms += 16)
{
    clock.Now = TimeSpan.FromMilliseconds(ms);
    root.Motion.Tick();
}

Console.WriteLine(element.Opacity); // 0.52

binding.Clear(MotionClearBehavior.HoldCurrent);
clock.Now = TimeSpan.FromMilliseconds(64);
root.Motion.Tick();
Console.WriteLine(element.Opacity); // 0.52 (with RestoreBase: 1)

sealed class ManualClock : IMotionClock
{
    public TimeSpan Now { get; set; }
}
```

## Remarks

Pass a value to `MotionPropertyBinding.Clear(MotionClearBehavior)`. The parameter defaults to `RestoreBase`.

`Clear` first cancels the active animation with `MotionCancelBehavior.KeepCurrent`, then stages one write that is applied on the next motion flush, and removes the binding from the motion graph:

- `RestoreBase` removes the binding's animation value. The property shows its value from the next lower source again, for example its default or aspect value. In the example, opacity returns to `1`.
- `HoldCurrent` writes the binding's current value into the animation source and leaves it there. In the example, opacity stays at `0.52` after the animation is gone.

The framework always clears with `RestoreBase`. This happens when the target element is detached or becomes effectively invisible, when the binding is disposed, and when generated markup cancels its motion executions.

The animation source is below a local value. If the property has a local value, for example `new UIElement { Opacity = 1f }`, that local value is shown instead of the animation, so neither behavior changes what the property shows.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `RestoreBase` | `0` | Removes the animation value so the property shows its underlying value again. This is the default. |
| `HoldCurrent` | `1` | Keeps the current animated value as the property's animation value. |

## Applies to

Cerneala motion property bindings.

## See also

- `Cerneala.UI.Motion.Properties.MotionPropertyBinding`
- `Cerneala.UI.Motion.Properties.MotionPropertyBinding_T_`
- `Cerneala.UI.Motion.Properties.MotionPropertyStore`
- `Cerneala.UI.Motion.Core.MotionCancelBehavior`
