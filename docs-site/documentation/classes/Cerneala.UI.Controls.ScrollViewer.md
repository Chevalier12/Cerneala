# ScrollViewer Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ScrollViewer.cs`

Hosts one content object in a clipped viewport and coordinates horizontal and vertical scroll bars for that content.

```csharp
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

`ScrollViewer` owns a `ScrollContentPresenter`, a horizontal `ScrollBar`, and a vertical `ScrollBar`. The presenter stores the scroll offsets, extent, and viewport values exposed through `ScrollInfo`; the scroll bars mirror those values during layout and when offsets change.

The default scroll policy disables horizontal scrolling and enables automatic vertical scrolling. `HorizontalScrollBarVisibility` defaults to `ScrollBarVisibility.Disabled`; `VerticalScrollBarVisibility` defaults to `ScrollBarVisibility.Auto`.

During measure and arrange, the control reevaluates whether each scroll bar is needed. `Visible` and `Hidden` reserve a `12` unit scroll-bar slot. `Auto` shows a scroll bar only when the presenter's extent is larger than its viewport. If one automatically visible scroll bar reduces the opposite viewport enough to require the other bar, the layout pass reevaluates both axes.

Mouse wheel input scrolls vertically by `48` units per wheel event sign when vertical scrolling is not disabled. The event is marked handled only when the vertical offset actually changes.

Offsets are clamped by `ScrollContentPresenter` to the valid `0..(extent - viewport)` range. Disabling an axis forces that axis offset back to `0`.

## Constructors

| Name | Description |
| --- | --- |
| `ScrollViewer()` | Initializes the owned presenter and scroll bars, collapses both scroll bars, attaches property synchronization handlers, adds the owned children to the logical and visual trees, and registers the mouse wheel handler. |

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
| `ScrollInfo` | `IScrollInfo` | Gets the owned presenter's scroll information and offset-setting API. |
| `Presenter` | `ScrollContentPresenter` | Gets the owned presenter that measures, clips, arranges, and offsets the content. |
| `HorizontalScrollBar` | `ScrollBar` | Gets the owned horizontal scroll bar. |
| `VerticalScrollBar` | `ScrollBar` | Gets the owned vertical scroll bar. |
| `IsHorizontalScrollBarVisible` | `bool` | Gets whether the horizontal scroll bar's `Visibility` is `Visible`. |
| `IsVerticalScrollBarVisible` | `bool` | Gets whether the vertical scroll bar's `Visibility` is `Visible`. |

## Methods

`ScrollViewer` does not declare public methods beyond inherited members.

## Events

`ScrollViewer` does not declare public events.

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
| Horizontal scrolling default | `Disabled` |
| Vertical scrolling default | `Auto` |
| Presenter available size | Deflated by reserved or visible scroll-bar slots, except when the corresponding available dimension is positive infinity. |
| Desired size with finite available size | Uses the finite available width and height. |
| Desired size with unbounded available size | Uses the presenter's desired size plus reserved scroll-bar thickness for visible/reserved bars. |

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
