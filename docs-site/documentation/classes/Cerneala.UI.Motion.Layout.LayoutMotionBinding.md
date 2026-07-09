# LayoutMotionBinding Class

## Definition
Namespace: `Cerneala.UI.Motion.Layout`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Layout/LayoutMotionBinding.cs`

Represents the active correction transform used by layout motion for a single `UIElement`.

```csharp
public sealed class LayoutMotionBinding : IDisposable
```

Inheritance:
`object` -> `LayoutMotionBinding`

Implements:
`IDisposable`

## Examples

Read the binding created for an element after a layout-motion change:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Motion.Layout;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

UIRoot root = new(100, 100);
Canvas canvas = new();
FixedElement child = new(new LayoutSize(20, 10))
{
    LayoutMotionId = "card",
    LayoutMotion = LayoutMotionOptions.Spring(
        MotionFactory.Tween<Cerneala.UI.Media.Transform>(TimeSpan.FromMilliseconds(100)))
};

root.VisualChildren.Add(canvas);
canvas.VisualChildren.Add(child);
root.ProcessFrame();

Canvas.SetLeft(child, 40);
root.ProcessFrame();

LayoutMotionBinding? binding = root.Motion.Layout.GetBinding(child);
bool isCorrecting = binding?.IsActive == true;

public sealed class FixedElement(LayoutSize desiredSize) : UIElement
{
    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        return desiredSize;
    }
}
```

## Remarks

`LayoutMotionBinding` is created internally by `LayoutMotionCoordinator` when an attached element participates in layout motion and its measured layout position changes. Callers normally access an existing binding with `MotionSystem.Layout.GetBinding(UIElement)`.

The binding owns a `MotionValue<Transform>` initialized to `Transform.Identity`. When layout motion starts, the coordinator jumps the value to an inverse correction transform and animates it back to identity with the element's `LayoutMotionOptions.CorrectionSpec`. The value subscription writes each sampled correction into the element's layout correction transform.

The correction is render-only. Tests for the layout-motion coordinator verify that ticking the animation updates motion frames without enqueueing additional measure or arrange work, preserving visual continuity while the element's arranged bounds already reflect the new layout position.

`Dispose` cancels and disposes the active motion handle, removes the correction subscription, and resets the element's layout correction transform to `Transform.Identity`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `CurrentCorrection` | `Transform` | Gets the current layout correction transform applied through the binding. |
| `IsActive` | `bool` | Gets whether the underlying correction value is currently animating. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Dispose()` | `void` | Releases the active correction motion and subscription, then resets the element correction transform to identity. |

## Applies to

Cerneala retained UI layout motion.

## See also

- `Cerneala.UI.Motion.Layout.LayoutMotionCoordinator`
- `Cerneala.UI.Motion.Layout.LayoutMotionOptions`
- `Cerneala.UI.Motion.Layout.LayoutMotionId`
- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Media.Transform`
