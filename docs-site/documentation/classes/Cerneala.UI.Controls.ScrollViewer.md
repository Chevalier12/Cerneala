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

## Remarks

`ScrollViewer` requires three active template parts: `PART_ScrollContentPresenter`, `PART_HorizontalScrollBar`, and `PART_VerticalScrollBar`. The presenter stores the scroll offsets, extent, and viewport values exposed through `ScrollInfo`; the active scroll bars mirror those values during layout and when offsets change. The public `Presenter`, `HorizontalScrollBar`, and `VerticalScrollBar` properties apply the current template and return those active parts.

The default component template uses a two-row, two-column `Grid`: the presenter occupies the star-sized upper-left cell, the vertical scroll bar occupies the auto-sized right cell, and the horizontal scroll bar occupies the auto-sized bottom cell. Assigning `null` to `ComponentTemplate` clears the local override and restores this default template.

The default scroll policy disables horizontal scrolling and enables automatic vertical scrolling. `HorizontalScrollBarVisibility` defaults to `ScrollBarVisibility.Disabled`; `VerticalScrollBarVisibility` defaults to `ScrollBarVisibility.Auto`.

During measure and arrange, the control reevaluates whether each scroll bar is needed through the template root. `Visible` and `Hidden` reserve the default scroll bar's `12` unit cross-axis size. `Auto` shows a scroll bar only when the presenter's extent is larger than its viewport. If one automatically visible scroll bar reduces the opposite viewport enough to require the other bar, the layout pass reevaluates both axes for at most three passes. A non-convergent state uses the conservative union of bars observed during those passes.

Template replacement unsubscribes the old presenter and scroll bars, releases content ownership from the old presenter, then connects and synchronizes the new parts. Missing parts and wrong part types fail during template application with the part name and expected type. Old parts cannot continue changing viewer offsets after a swap.

Visibility changes performed during convergence normally enqueue ancestor layout work. Because the viewer immediately measures and arranges that same active template hierarchy, it consumes only those measure/arrange entries while applying the visibility change. Render, hit-test, input, and other invalidations remain intact; the next unchanged frame therefore has no stale layout work.

Mouse wheel input scrolls vertically by `48` units per wheel event sign when vertical scrolling is not disabled. The event is marked handled only when the vertical offset actually changes.

The active scroll bars use a `SmallChange` of `48` units. Pressing or holding either direction `RepeatButton` therefore scrolls by one visible line step per click or repeat tick.

Offsets are clamped by `ScrollContentPresenter` to the valid `0..(extent - viewport)` range. Disabling an axis forces that axis offset back to `0`.

## Constructors

| Name | Description |
| --- | --- |
| `ScrollViewer()` | Registers mouse wheel handling and applies the default grid component template containing the three required parts. |

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
| Horizontal scrolling default | `Disabled` |
| Vertical scrolling default | `Auto` |
| Presenter available size | The star-sized grid cell remaining after visible or hidden auto-sized scroll-bar cells reserve space. |
| Desired size with finite available size | Uses the template grid's finite desired size. |
| Desired size with unbounded available size | Star grid tracks use child desired sizes, preserving the presenter's content-driven desired size plus visible/reserved bars. |

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
