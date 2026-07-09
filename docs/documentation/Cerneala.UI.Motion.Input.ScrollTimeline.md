# ScrollTimeline Class

## Definition
Namespace: `Cerneala.UI.Motion.Input`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Input/ScrollTimeline.cs`

Exposes normalized scroll progress values for a `ScrollViewer` so motion bindings can follow the current scroll position.

```csharp
public sealed class ScrollTimeline
```

Inheritance:
`object` -> `ScrollTimeline`

## Examples

Create a scroll timeline after the `ScrollViewer` is attached to a `UIRoot`, then bind vertical progress to an element opacity.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion;

UIRoot root = new(100, 100);
ScrollViewer scrollViewer = new()
{
    Content = new FixedElement(new LayoutSize(100, 300))
};
UIElement header = new();

root.VisualChildren.Add(scrollViewer);
root.VisualChildren.Add(header);
root.ProcessFrame();

ScrollTimeline timeline = scrollViewer.Motion().ScrollTimeline();
header.Motion().Opacity.Bind(timeline.Progress.Map(1f, 0f));

scrollViewer.ScrollInfo.SetVerticalOffset(100);
timeline.Update();

float progress = timeline.Progress.Current;
float opacity = header.Opacity;

sealed class FixedElement(LayoutSize desiredSize) : UIElement
{
    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        return desiredSize;
    }
}
```

## Remarks

`ScrollTimeline` is created through `MotionElementFacade.ScrollTimeline()` for `ScrollViewer` instances. The underlying constructor is internal, so application code uses `scrollViewer.Motion().ScrollTimeline()` rather than constructing the timeline directly.

The timeline reads the `ScrollViewer.ScrollInfo` offsets and dimensions. `Update` normalizes vertical scroll as `VerticalOffset / (ExtentHeight - ViewportHeight)` and horizontal scroll as `HorizontalOffset / (ExtentWidth - ViewportWidth)`, then clamps each result to the range `0` through `1`. If the extent does not exceed the viewport, the corresponding progress value is `0`.

`Progress` tracks vertical scroll progress, while `HorizontalProgress` tracks horizontal scroll progress. The raw `VerticalOffset` and `HorizontalOffset` properties expose the current offsets from the same scroll information.

Scroll-linked bindings can update render-only properties without layout invalidation. Binding a scroll timeline to a layout-affecting property requires the binding's explicit `AllowLayout()` opt-in.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Progress` | `ScrollTimelineProgress` | Gets the normalized vertical scroll progress value. |
| `HorizontalProgress` | `ScrollTimelineProgress` | Gets the normalized horizontal scroll progress value. |
| `VerticalOffset` | `float` | Gets the current vertical offset from the wrapped `ScrollViewer.ScrollInfo`. |
| `HorizontalOffset` | `float` | Gets the current horizontal offset from the wrapped `ScrollViewer.ScrollInfo`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Update()` | `void` | Reads the current scroll information, normalizes vertical and horizontal offsets, and jumps the progress values to the latest clamped positions. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `MotionElementFacade.ScrollTimeline()` | `InvalidOperationException` | The motion facade target is not a `ScrollViewer`. |
| `MotionElementFacade.ScrollTimeline()` | `InvalidOperationException` | The `ScrollViewer` is not attached to a root, so the timeline cannot create motion values from a root motion graph. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.MotionElementFacade`
- `Cerneala.UI.Motion.Input.ScrollTimelineProgress`
- `Cerneala.UI.Motion.Input.ScrollMotionBinding<T>`
- `Cerneala.UI.Controls.ScrollViewer`
