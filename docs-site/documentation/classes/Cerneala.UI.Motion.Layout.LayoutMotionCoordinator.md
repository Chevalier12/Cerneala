# LayoutMotionCoordinator Class

## Definition
Namespace: `Cerneala.UI.Motion.Layout`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Layout/LayoutMotionCoordinator.cs`

Coordinates snapshot-based layout motion for elements that opt into layout correction.

```csharp
public sealed class LayoutMotionCoordinator
```

Inheritance:
`object` -> `LayoutMotionCoordinator`

## Examples

Inspect the layout-motion binding created for an element after a layout change:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Motion.Layout;
using Cerneala.UI.Motion.Specs;

UIRoot root = new(100, 100);
Canvas canvas = new();
UIElement card = new FixedElement(new LayoutSize(20, 10))
{
    LayoutMotionId = "card",
    LayoutMotion = LayoutMotionOptions.Spring(
        Motion.Tween<Cerneala.UI.Media.Transform>(TimeSpan.FromMilliseconds(100)))
};

root.VisualChildren.Add(canvas);
canvas.VisualChildren.Add(card);
root.ProcessFrame();

Canvas.SetLeft(card, 40);
root.ProcessFrame();

LayoutMotionBinding? binding = root.Motion.Layout.GetBinding(card);
bool isCorrecting = binding?.IsActive == true;

sealed class FixedElement(LayoutSize desiredSize) : UIElement
{
    protected override LayoutSize MeasureCore(MeasureContext context) => desiredSize;
}
```

## Remarks

`LayoutMotionCoordinator` is created by `MotionSystem` and exposed through `MotionSystem.Layout`. The retained frame pipeline calls it from `MotionFrameCoordinator.BeforeLayout` and `MotionFrameCoordinator.AfterLayout`; application code normally reads coordinator state rather than invoking the internal snapshot methods directly.

The coordinator participates only when the root layout queue has work and the reduced-motion policy is not `ReducedMotionMode.DisableNonEssential`. It walks the visual tree, records first and last root-space bounds for attached elements whose `UIElement.LayoutMotion` and `UIElement.LayoutMotionId` are both set, then starts a `LayoutMotionBinding` correction when the valid bounds changed.

Corrections are render-scope transforms, not layout mutations. A layout move starts an inverse correction from the previous visual bounds toward `Transform.Identity`, so motion frames can preserve visual continuity without enqueueing new measure or arrange work. The binding writes through `UIElement.SetLayoutCorrectionTransform`, which invalidates render scope only.

The coordinator keeps previous snapshots by `LayoutMotionId` so an element moved between parents can retain continuity when the same element instance and id are observed across frames. Bounds with non-positive size or non-finite coordinates are ignored.

`GetBinding` returns an existing binding when correction work has been created for the element. It does not create bindings for elements that have not produced layout motion.

## Constructors

| Name | Description |
| --- | --- |
| `LayoutMotionCoordinator(MotionSystem motion)` | Initializes a coordinator owned by the supplied motion system. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ActiveBindingCount` | `int` | Gets the number of element-to-binding entries currently tracked by the coordinator. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetBinding(UIElement element)` | `LayoutMotionBinding?` | Returns the existing layout-motion binding for `element`, or `null` when no binding has been created. |

## Method Details

### GetBinding

```csharp
public LayoutMotionBinding? GetBinding(UIElement element)
```

Looks up the binding for an attached element after layout-motion correction work has created one. The method validates `element` and returns `null` instead of allocating a new binding.

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `LayoutMotionCoordinator(MotionSystem)` | `ArgumentNullException` | `motion` is `null`. |
| `GetBinding(UIElement)` | `ArgumentNullException` | `element` is `null`. |

## Applies to

Cerneala retained UI layout-motion processing.

## See also

- `Cerneala.UI.Motion.Core.MotionSystem`
- `Cerneala.UI.Motion.Core.MotionFrameCoordinator`
- `Cerneala.UI.Motion.Layout.LayoutMotionBinding`
- `Cerneala.UI.Motion.Layout.LayoutMotionOptions`
- `Cerneala.UI.Motion.Layout.LayoutMotionId`
- `Cerneala.UI.Elements.UIElement`
