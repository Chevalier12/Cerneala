# ScrollViewer Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ScrollViewer.cs`

Hosts one content object in a clipped viewport and coordinates horizontal and vertical scroll bars for that content.

```csharp
[TemplatePart("PART_ScrollContentPresenter", typeof(ScrollContentPresenter))]
[TemplatePart("PART_HorizontalScrollBar", typeof(ScrollBar))]
[TemplatePart("PART_VerticalScrollBar", typeof(ScrollBar))]
public class ScrollViewer : Control
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ScrollViewer`

## Examples

Create a scroll viewer with automatic vertical scrolling and programmatically move the viewport:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

ScrollViewer viewer = new()
{
    Content = new DocumentElement(),
    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
};

viewer.Measure(new MeasureContext(new LayoutSize(320, 180)));
viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 320, 180)));

viewer.ScrollInfo.SetVerticalOffset(96);
float currentOffset = viewer.ScrollInfo.VerticalOffset;

internal sealed class DocumentElement : UIElement
{
    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        return new LayoutSize(320, 720);
    }
}
```

Give the viewer a background and an inward 1-unit border (a negative side is a band inside the viewer; a positive side is painted outside it and takes no room). The default template draws both and lays out the presenter and scroll bars inside the border:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

ScrollViewer viewer = new()
{
    Background = new SolidColorBrush(new Color(22, 28, 32)),
    BorderBrush = new SolidColorBrush(new Color(44, 55, 62)),
    BorderThickness = new Thickness(-1),
    VerticalScrollBarVisibility = ScrollBarVisibility.Visible
};

viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));
viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

// viewer.Presenter.ArrangedBounds         == (1, 1, 88, 98)
// viewer.VerticalScrollBar.ArrangedBounds == (89, 1, 10, 98)
```

## Remarks

`ScrollViewer` requires three active template parts: `PART_ScrollContentPresenter`, `PART_HorizontalScrollBar`, and `PART_VerticalScrollBar`. The presenter stores the scroll offsets, extent, and viewport values exposed through `ScrollInfo`; the active scroll bars mirror those values during layout and when offsets change. The public `Presenter`, `HorizontalScrollBar`, and `VerticalScrollBar` properties apply the current template and return those active parts.

The default component template uses a two-row, two-column `Grid`: the presenter occupies the star-sized upper-left cell, the vertical scroll bar occupies the auto-sized right cell, and the horizontal scroll bar occupies the auto-sized bottom cell. Assigning `null` to `ComponentTemplate` clears the local override and restores this default template.

The scroll bar parts exist as soon as the viewer's template is applied (reading `Presenter`, `HorizontalScrollBar` or `VerticalScrollBar` applies it), but a scroll bar builds its own template, with `Track`, `Thumb` and the direction `RepeatButton` elements, only at its first measure, as in WPF, so a bar that is `Collapsed` has none. For example, a viewer with `VerticalScrollBarVisibility = ScrollBarVisibility.Hidden`, or an `Auto` viewer whose content fits, has `VerticalScrollBar.ComponentTemplateInstance == null` and no `Track`, `Thumb` or `RepeatButton` below it. When an `Auto` bar becomes `Visible` because the content grew, it builds its template in the layout of that frame and is laid out and drawn in that frame. Reading `VerticalScrollBar.Track` builds the bar's template on demand. See `ScrollBar`.

The default template's root is a `Border` that wraps that grid. The viewer's `Background`, `BorderBrush`, and `BorderThickness` are relayed to this border with `UiPropertyValueSource.TemplateOwnerBinding`, so they win over `Aspect` rules that target `Border`. The border fills `Background` over the whole viewer, paints `BorderBrush` with each side of its own signed `BorderThickness` (a negative side `-t` inside the viewer's edge, a positive side `+t` outside it), and arranges the grid inset by the negative sides only: `-1` insets the grid by 1 unit, `1` insets it by nothing and paints the border outside the viewer. With a negative border, the presenter and scroll bars sit inside the border, and the presenter's viewport clip stays inside it too. With the default values (`null`, `null`, `Thickness.Zero`) the template draws nothing extra and adds no inset. `Padding` is not used by the default template. A custom `ComponentTemplate` decides itself whether to use these properties.

The default scroll policy disables horizontal scrolling and enables automatic vertical scrolling. `HorizontalScrollBarVisibility` defaults to `ScrollBarVisibility.Disabled`; `VerticalScrollBarVisibility` defaults to `ScrollBarVisibility.Auto`.

During measure and arrange, the control reevaluates whether each scroll bar is needed through the template root. `Visible` reserves the default scroll bar's cross-axis size, which is `10`: the template's `12` minus the 1-unit default border of the scroll bar on each side, which is a positive side painted outside the bar. `Hidden` and `Disabled` collapse the bar, as WPF's computed scroll bar visibility does, so it takes no space; with `Hidden` the presenter still scrolls on that axis. `Auto` shows a scroll bar only when the presenter's extent is larger than its viewport. If one automatically visible scroll bar reduces the opposite viewport enough to require the other bar, the layout pass reevaluates both axes for at most three passes. A non-convergent state uses the conservative union of bars observed during those passes.

The presenter desires, on each axis, the smaller of its viewport and its extent, as WPF's `ScrollContentPresenter` does, so a viewer whose content fits asks only for the content size and not for the whole available space. The visible or reserved scroll bars and `BorderThickness` are added to that size. For example, a viewer measured with an available size of 100 x 100:

- With content that desires 80 x 20 and both `HorizontalScrollBarVisibility` and `VerticalScrollBarVisibility` set to `Auto`, `DesiredSize` is 80 x 20 and no scroll bar is visible.
- With content that desires 80 x 300 and `VerticalScrollBarVisibility = ScrollBarVisibility.Auto`, the vertical scroll bar is visible, `ScrollInfo.ExtentHeight` is `300`, `ScrollInfo.ViewportHeight` is `100`, the presenter desires 80 x 100, and `DesiredSize` is 90 x 100: the 80-unit content width plus the 10-unit scroll bar.

The viewport is still the presenter's available size during measure and its final size during arrange, so a parent that arranges the viewer larger than `DesiredSize` gets a larger viewport.

During arrange, the viewer measures its template again with the final size, except on an axis where the final size is exactly the size the template desired: there it keeps the constraint of its own measure on that axis, as WPF does. A left-aligned viewer in a `StackPanel` therefore grows when its content grows, with `Auto`, `Hidden` and `Disabled` horizontal policies alike. For example, a viewer measured with width `300` and arranged at its desired width `18` keeps `300` as the constraint, so the presenter can desire the grown content width `86`; with `18` it would desire `18` again and the viewer would not grow.

Template replacement unsubscribes the old presenter and scroll bars, releases content ownership from the old presenter, then connects and synchronizes the new parts. Missing parts and wrong part types fail during template application with the part name and expected type. Old parts cannot continue changing viewer offsets after a swap.

Visibility changes performed during convergence normally enqueue layout work along the visual ancestor chain. The viewer consumes measure and arrange entries only from the changed scroll bar through its own template boundary, because that hierarchy is laid out synchronously. It also increments the layout version of every template element between the scroll bar and the viewer, so the default `Border` and its `Grid` (or any deeper custom template nesting) re-arrange the newly shown or hidden bar. Work for parents outside the viewer remains scheduled so an outer grid or panel can recompute the viewer's slot. Render, hit-test, input, and other invalidations remain intact; the next unchanged frame therefore has no stale internal layout work.

Mouse wheel input scrolls vertically by `48` units per wheel event sign when vertical scrolling is not disabled. The event is marked handled only when the vertical offset actually changes.

The active scroll bars use a `SmallChange` of `48` units. Pressing or holding either direction `RepeatButton` therefore scrolls by one visible line step per click or repeat tick.

The active scroll bars use a `LargeChange` equal to the presenter's viewport on their axis: `ViewportHeight` for the vertical bar and `ViewportWidth` for the horizontal bar. A click on the track before or after the thumb therefore scrolls by one page, the visible viewport size, and the offset stops at `0` or at `extent - viewport`. The viewer updates `LargeChange` with `ViewportSize` during layout, so the page follows the viewport when the viewer is resized. For example, a viewer with `Height = 100` around 1000-unit-high content moves from vertical offset `0` to `100` on one click below the thumb, and back to `0` on one click above it.

Offsets are clamped by `ScrollContentPresenter` to the valid `0..(extent - viewport)` range. Disabling an axis forces that axis offset back to `0`.

As WPF's `ScrollViewer`, the viewer handles `UIElement.RequestBringIntoViewEvent`, which `UIElement.BringIntoView` raises, when the requesting element is inside the presenter's content. It marks the event handled, calls `Presenter.MakeVisible` with the element and the requested rectangle, and then calls its own `BringIntoView` with the part that is visible after the scroll, in the viewer's coordinates, so the `ScrollViewer`s around it scroll too. A request from the viewer itself or from an element outside the content, such as a scroll bar, bubbles on unchanged, and a handler that marked the event handled before it reached the viewer stops it. For example, a `Button` 20 high, 300 pixels down in an inner `ScrollViewer` with `Height = 100` that starts 200 pixels down in an outer `ScrollViewer` 100 high:

```csharp
button.BringIntoView();
// inner.Presenter.VerticalOffset is 220 (the Button ends at 320);
// outer.Presenter.VerticalOffset is 200 (the visible Button is at 280..300 in the outer content).
```

## Constructors

| Name | Description |
| --- | --- |
| `ScrollViewer()` | Registers mouse wheel and `RequestBringIntoView` handling and applies the default component template: a `Border` around a grid containing the three required parts. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `HorizontalScrollBarVisibilityProperty` | `UiProperty<ScrollBarVisibility>` | Identifies `HorizontalScrollBarVisibility`. The default value is `Disabled`; metadata affects measure, arrange, and render. |
| `VerticalScrollBarVisibilityProperty` | `UiProperty<ScrollBarVisibility>` | Identifies `VerticalScrollBarVisibility`. The default value is `Auto`; metadata affects measure, arrange, and render. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Content` | `object?` | Gets or sets the content hosted by the scroll presenter. Changing the value updates `Presenter.Content`, increments the layout version, and invalidates measure and render when the value changes by `ContentControl.ContentEqualityComparer`. |
| `HorizontalScrollBarVisibility` | `ScrollBarVisibility` | Gets or sets the horizontal scroll-bar policy. The default is `Disabled`. |
| `VerticalScrollBarVisibility` | `ScrollBarVisibility` | Gets or sets the vertical scroll-bar policy. The default is `Auto`. |
| `ScrollInfo` | `IScrollInfo` | Gets the active presenter's scroll information and offset-setting API. |
| `Presenter` | `ScrollContentPresenter` | Gets `PART_ScrollContentPresenter` from the active template. |
| `HorizontalScrollBar` | `ScrollBar` | Gets `PART_HorizontalScrollBar` from the active template. |
| `VerticalScrollBar` | `ScrollBar` | Gets `PART_VerticalScrollBar` from the active template. |
| `IsHorizontalScrollBarVisible` | `bool` | Gets whether the horizontal scroll bar's `Visibility` is `Visible`. |
| `IsVerticalScrollBarVisible` | `bool` | Gets whether the vertical scroll bar's `Visibility` is `Visible`. |

## Methods

`ScrollViewer` does not declare public methods beyond inherited members.

## Events

| Name | Event Type | Description |
| --- | --- | --- |
| `ScrollChanged` | `EventHandler<ScrollChangedEventArgs>` | Routed bubble event raised when the active presenter's horizontal or vertical offset changes. The event reports old and new offsets. |

## Scroll Bar Visibility

| Value | Behavior in `ScrollViewer` |
| --- | --- |
| `Disabled` | Disables scrolling on that axis, collapses the scroll bar, and coerces the offset to `0`. |
| `Auto` | Enables scrolling on that axis and shows the scroll bar only when the extent exceeds the viewport. |
| `Hidden` | Enables scrolling on that axis and collapses the scroll bar (`Visibility.Collapsed`), so it takes no layout space. |
| `Visible` | Enables scrolling on that axis and keeps the scroll bar visible, reserving layout space. |

## Layout Behavior

| Behavior | Value |
| --- | --- |
| Scroll bar thickness | `12` layout units |
| Mouse wheel scroll amount | `48` layout units per wheel sign |
| Direction button scroll amount | `48` layout units per click or repeat tick |
| Track click scroll amount | One viewport per click: `ViewportHeight` on the vertical bar, `ViewportWidth` on the horizontal bar |
| Horizontal scrolling default | `Disabled` |
| Vertical scrolling default | `Auto` |
| Border inset | The template grid is arranged inside the negative sides of the viewer's `BorderThickness`: `-t` on a side insets it by `t`; a positive or zero side insets it by nothing. |
| Presenter available size | The star-sized grid cell remaining after the border inset and the visible auto-sized scroll-bar cells reserve space. Collapsed bars (`Disabled`, `Hidden`, or `Auto` with content that fits) reserve none. |
| Desired size with finite available size | Uses the template grid's finite desired size plus the negative sides of `BorderThickness`. The presenter cell desires, per axis, the smaller of the viewport and the extent, so content 80 x 20 in 100 x 100 gives 80 x 20, and content 80 x 300 with a visible vertical bar gives 90 x 100. |
| Desired size with unbounded available size | Star grid tracks use child desired sizes, preserving the presenter's content-driven desired size plus visible/reserved bars and `BorderThickness`. |

## Property Information

| Property | Identifier field | Default value | Metadata/options |
| --- | --- | --- | --- |
| `HorizontalScrollBarVisibility` | `HorizontalScrollBarVisibilityProperty` | `ScrollBarVisibility.Disabled` | `UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender` |
| `VerticalScrollBarVisibility` | `VerticalScrollBarVisibilityProperty` | `ScrollBarVisibility.Auto` | `UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender` |

## Applies to

`Cerneala.UI.Controls.ScrollViewer` in the `Cerneala` project.

## See also

- `Cerneala.UI.Controls.ScrollContentPresenter`
- `Cerneala.UI.Controls.IScrollInfo`
- `Cerneala.UI.Controls.ScrollBarVisibility`
- `Cerneala.UI.Controls.Primitives.ScrollBar`
- `Cerneala.UI.Controls.Control`
