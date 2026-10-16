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

Give the viewer a background and a 1-unit border. The default template draws both and lays out the presenter and scroll bars inside the border:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

ScrollViewer viewer = new()
{
    Background = new SolidColorBrush(new Color(22, 28, 32)),
    BorderBrush = new SolidColorBrush(new Color(44, 55, 62)),
    BorderThickness = new Thickness(1),
    VerticalScrollBarVisibility = ScrollBarVisibility.Visible
};

viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));
viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

// viewer.Presenter.ArrangedBounds         == (1, 1, 86, 98)
// viewer.VerticalScrollBar.ArrangedBounds == (87, 1, 12, 98)
```

## Remarks

`ScrollViewer` requires three active template parts: `PART_ScrollContentPresenter`, `PART_HorizontalScrollBar`, and `PART_VerticalScrollBar`. The presenter stores the scroll offsets, extent, and viewport values exposed through `ScrollInfo`; the active scroll bars mirror those values during layout and when offsets change. The public `Presenter`, `HorizontalScrollBar`, and `VerticalScrollBar` properties apply the current template and return those active parts.

The default component template uses a two-row, two-column `Grid`: the presenter occupies the star-sized upper-left cell, the vertical scroll bar occupies the auto-sized right cell, and the horizontal scroll bar occupies the auto-sized bottom cell. Assigning `null` to `ComponentTemplate` clears the local override and restores this default template.

The default template's root is a `Border` that wraps that grid. The viewer's `Background`, `BorderBrush`, and `BorderThickness` are relayed to this border with `UiPropertyValueSource.TemplateOwnerBinding`, so they win over `Aspect` rules that target `Border`. The border fills `Background` over the whole viewer, strokes `BorderBrush` with the largest side of `BorderThickness`, and arranges the grid inset by `BorderThickness` on each side. The presenter and scroll bars therefore sit inside the border, and the presenter's viewport clip stays inside it too. With the default values (`null`, `null`, `Thickness.Zero`) the template draws nothing extra and adds no inset. `Padding` is not used by the default template. A custom `ComponentTemplate` decides itself whether to use these properties.

The default scroll policy disables horizontal scrolling and enables automatic vertical scrolling. `HorizontalScrollBarVisibility` defaults to `ScrollBarVisibility.Disabled`; `VerticalScrollBarVisibility` defaults to `ScrollBarVisibility.Auto`.

During measure and arrange, the control reevaluates whether each scroll bar is needed through the template root. `Visible` and `Hidden` reserve the default scroll bar's `12` unit cross-axis size. `Auto` shows a scroll bar only when the presenter's extent is larger than its viewport. If one automatically visible scroll bar reduces the opposite viewport enough to require the other bar, the layout pass reevaluates both axes for at most three passes. A non-convergent state uses the conservative union of bars observed during those passes.

Template replacement unsubscribes the old presenter and scroll bars, releases content ownership from the old presenter, then connects and synchronizes the new parts. Missing parts and wrong part types fail during template application with the part name and expected type. Old parts cannot continue changing viewer offsets after a swap.

Visibility changes performed during convergence normally enqueue layout work along the visual ancestor chain. The viewer consumes measure and arrange entries only from the changed scroll bar through its own template boundary, because that hierarchy is laid out synchronously. It also increments the layout version of every template element between the scroll bar and the viewer, so the default `Border` and its `Grid` (or any deeper custom template nesting) re-arrange the newly shown or hidden bar. Work for parents outside the viewer remains scheduled so an outer grid or panel can recompute the viewer's slot. Render, hit-test, input, and other invalidations remain intact; the next unchanged frame therefore has no stale internal layout work.

Mouse wheel input scrolls vertically by `48` units per wheel event sign when vertical scrolling is not disabled. The event is marked handled only when the vertical offset actually changes.

The active scroll bars use a `SmallChange` of `48` units. Pressing or holding either direction `RepeatButton` therefore scrolls by one visible line step per click or repeat tick.

The active scroll bars use a `LargeChange` equal to the presenter's viewport on their axis: `ViewportHeight` for the vertical bar and `ViewportWidth` for the horizontal bar. A click on the track before or after the thumb therefore scrolls by one page, the visible viewport size, and the offset stops at `0` or at `extent - viewport`. The viewer updates `LargeChange` with `ViewportSize` during layout, so the page follows the viewport when the viewer is resized. For example, a viewer with `Height = 100` around 1000-unit-high content moves from vertical offset `0` to `100` on one click below the thumb, and back to `0` on one click above it.

Offsets are clamped by `ScrollContentPresenter` to the valid `0..(extent - viewport)` range. Disabling an axis forces that axis offset back to `0`.

## Constructors

| Name | Description |
| --- | --- |
| `ScrollViewer()` | Registers mouse wheel handling and applies the default component template: a `Border` around a grid containing the three required parts. |

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
| `Hidden` | Enables scrolling on that axis and reserves scroll-bar layout space while keeping the scroll bar hidden. |
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
| Border inset | The template grid is arranged inside the viewer's `BorderThickness` on each side. |
| Presenter available size | The star-sized grid cell remaining after the border inset and the visible or hidden auto-sized scroll-bar cells reserve space. |
| Desired size with finite available size | Uses the template grid's finite desired size plus `BorderThickness`. |
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
