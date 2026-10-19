# IScrollInfo Interface

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/IScrollInfo.cs`

Exposes the scroll offsets, the extent, and the viewport of a scrolling surface, and lets code change the offsets.

```csharp
public interface IScrollInfo
```

## Examples

Scroll a `ScrollViewer` back to the top and read how far it can scroll:

```csharp
using Cerneala.UI.Controls;

ScrollViewer viewer = new() { Content = new StackPanel() };

IScrollInfo info = viewer.ScrollInfo;
info.SetVerticalOffset(0);

float maxVerticalOffset = MathF.Max(0, info.ExtentHeight - info.ViewportHeight);
```

## Remarks

In the framework, `ScrollContentPresenter` is the only implementation. `ScrollViewer.ScrollInfo` returns the viewer's `ScrollContentPresenter` (template part `PART_ScrollContentPresenter`); reading it applies the template first and throws `InvalidOperationException` when the template has no such part.

`ScrollViewer` sets `CanHorizontallyScroll` and `CanVerticallyScroll` on its presenter from its scroll bar visibility during measure, arrange, and template application. The value is `false` only for `ScrollBarVisibility.Disabled`. For example, with the default `HorizontalScrollBarVisibility = Disabled` and `VerticalScrollBarVisibility = Auto`, the presenter gets `CanHorizontallyScroll = false` and `CanVerticallyScroll = true`.

In `ScrollContentPresenter`:

- `SetHorizontalOffset` and `SetVerticalOffset` store `0` when scrolling in that direction is not allowed.
- Otherwise the offset is clamped to the range `0` to `max(0, extent - viewport)`. For example, with `ExtentHeight = 500` and `ViewportHeight = 200`, `SetVerticalOffset(1000)` stores `300` and `SetVerticalOffset(-10)` stores `0`.
- Before the first layout pass the extent and viewport are `0`, so every offset is clamped to `0`.
- `NaN` is rejected with `ArgumentException`.
- Changing `CanHorizontallyScroll` or `CanVerticallyScroll` invalidates the presenter's measure, so the next layout measures the content with the constraint of the new value. Setting the current value again does nothing.
- Content is measured with infinite size in each direction that can scroll.

Other framework code reads this interface too: `ItemsPresenter.UpdateVirtualizationFromScrollInfo` reads `ViewportHeight` and `VerticalOffset`, and `ScrollTimeline` reads the offset, extent, and viewport of a `ScrollViewer`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `HorizontalOffset` | `float` | Gets the current horizontal scroll offset. |
| `VerticalOffset` | `float` | Gets the current vertical scroll offset. |
| `ExtentWidth` | `float` | Gets the width of the scrollable content. |
| `ExtentHeight` | `float` | Gets the height of the scrollable content. |
| `ViewportWidth` | `float` | Gets the width of the visible area. |
| `ViewportHeight` | `float` | Gets the height of the visible area. |
| `CanHorizontallyScroll` | `bool` | Gets or sets whether horizontal scrolling is allowed. |
| `CanVerticallyScroll` | `bool` | Gets or sets whether vertical scrolling is allowed. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `SetHorizontalOffset(float offset)` | `void` | Requests a new horizontal offset. The implementation decides the stored value. |
| `SetVerticalOffset(float offset)` | `void` | Requests a new vertical offset. The implementation decides the stored value. |

## Applies to

`Cerneala.UI.Controls.ScrollViewer` and `Cerneala.UI.Controls.ScrollContentPresenter`.

## See also

- `Cerneala.UI.Controls.ScrollViewer`
- `Cerneala.UI.Controls.ScrollContentPresenter`
- `Cerneala.UI.Controls.ScrollBarVisibility`
- `Cerneala.UI.Controls.ItemsPresenter`
