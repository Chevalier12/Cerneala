# ScrollTimelineProgress Class

## Definition

Namespace: `Cerneala.UI.Motion.Input`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Input/ScrollTimeline.cs`

Represents normalized scroll progress from a `ScrollTimeline` and creates bindings that map that progress into float property values.

```csharp
public sealed class ScrollTimelineProgress
```

Inheritance:
`object` -> `ScrollTimelineProgress`

## Examples

Map vertical scroll progress to opacity and scale values:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion;

UIRoot root = new(100, 100);
ScrollViewer scrollViewer = new()
{
    Content = new StackPanel()
};

Border header = new();
Border progressBar = new()
{
    RenderTransformOrigin = new LayoutPoint(0, 0.5f)
};

root.VisualChildren.Add(scrollViewer);
root.VisualChildren.Add(header);
root.VisualChildren.Add(progressBar);
root.ProcessFrame();

ScrollTimeline timeline = scrollViewer.Motion().ScrollTimeline();
header.Motion().Opacity.Bind(timeline.Progress.Map(1f, 0.6f));
progressBar.Motion().Animate(UIElement.ScaleXProperty).Bind(timeline.Progress.Map(0.04f, 1f));

scrollViewer.ScrollInfo.SetVerticalOffset(100);
timeline.Update();

float normalizedProgress = timeline.Progress.Current;
```

## Remarks

`ScrollTimelineProgress` instances are exposed by `ScrollTimeline.Progress` for vertical scrolling and `ScrollTimeline.HorizontalProgress` for horizontal scrolling. The public API does not construct this class directly; `ScrollTimeline` creates it from graph-backed `MotionValue<float>` values.

`Current` returns the latest normalized progress value. `ScrollTimeline.Update()` computes that value from the owning `ScrollViewer` scroll offsets and clamps it to the `0` to `1` range. When the scrollable extent is not larger than the viewport, progress is `0`.

Use `Map(float from, float to)` to create a `ScrollMotionBinding<float>`. The returned binding maps normalized progress from `0` to `1` into the supplied output range, then can be bound through `MotionPropertyShortcut<T>.Bind` or `MotionAnimationBuilder<T>.Bind`.

Bindings created from this class update when the underlying progress value changes. Call `ScrollTimeline.Update()` after scroll offsets change so bound properties receive the new mapped value.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Current` | `float` | Gets the current normalized scroll progress. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Map(float from, float to)` | `ScrollMotionBinding<float>` | Creates a float binding that maps progress from the `0` to `1` scroll range into the supplied output range. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Input.ScrollTimeline`
- `Cerneala.UI.Motion.Input.ScrollMotionBinding<T>`
- `Cerneala.UI.Motion.Input.MotionRange`
- `Cerneala.UI.Motion.MotionElementFacade`
- `Cerneala.UI.Controls.ScrollViewer`
