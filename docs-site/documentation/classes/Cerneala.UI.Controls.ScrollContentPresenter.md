# ScrollContentPresenter Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ScrollContentPresenter.cs`

Hosts scrollable content for `ScrollViewer`, tracks extent and viewport metrics, and applies offset-based arrangement with clipping.

```csharp
public class ScrollContentPresenter : ContentControl, IScrollInfo
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ContentControl` -> `ScrollContentPresenter`

Implements:
`IScrollInfo`

## Examples
Create a presenter, measure content larger than the viewport, and move the visible window by setting offsets.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

ScrollContentPresenter presenter = new()
{
    Content = new UIElement()
};

presenter.Measure(new MeasureContext(new LayoutSize(80, 50)));
presenter.SetHorizontalOffset(20);
presenter.SetVerticalOffset(10);
presenter.Arrange(new ArrangeContext(new LayoutRect(0, 0, 80, 50)));

float horizontalOffset = presenter.HorizontalOffset;
float verticalOffset = presenter.VerticalOffset;
```

Disable an axis when content should be constrained to the viewport size on that axis.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

ScrollContentPresenter presenter = new()
{
    CanHorizontallyScroll = false,
    CanVerticallyScroll = true,
    Content = new UIElement()
};
```

## Remarks
`ScrollContentPresenter` is the content host used by `ScrollViewer`. It exposes the `IScrollInfo` metrics that scroll bars and wheel handling use: current offsets, total content extent, viewport size, and per-axis scroll enablement.

During measure, scroll-enabled axes are measured with an infinite available size so content can report its natural extent. Disabled axes are measured with the available viewport dimension. One exception: the text view of a `TextBox` or `PasswordBox` template is always measured with the available width, as WPF's `ScrollContentPresenter` measures its `TextBoxView` content, so that text wraps at the viewport width. Its desired width is its widest line, so a word that `TextWrapping.WrapWithOverflow` keeps whole still makes the extent wider than the viewport and scrolls horizontally. For example, a `TextBox` with `Width="100"`, `TextWrapping="WrapWithOverflow"` and the text `one two three four five six seven` shows several lines, and `ExtentWidth` stays within the viewport. The measured content size becomes `ExtentWidth` and `ExtentHeight`; the finite available size becomes `ViewportWidth` and `ViewportHeight`, and an infinite available dimension makes the viewport equal to the extent on that axis.

As in WPF's `ScrollContentPresenter`, the desired size on each axis is the smaller of the viewport and the extent, so content that fits does not ask for the whole available space. The viewport stays the available size during measure, and arrange then sets it to the final size. For example, measured with an available size of 100 x 100:

- Content that desires 80 x 20 gives `DesiredSize` 80 x 20, with `ViewportWidth` and `ViewportHeight` `100`.
- Content that desires 80 x 300 gives `DesiredSize` 80 x 100, with `ExtentHeight` `300` and `ViewportHeight` `100`.

Changing `CanHorizontallyScroll` or `CanVerticallyScroll` invalidates the presenter's measure, as in WPF, also when the presenter's own available size stays the same. For example, a `ScrollViewer` 200 x 320 whose content desires 200 x 320 measures that content with height `320` while `VerticalScrollBarVisibility` is `Disabled`. After `VerticalScrollBarVisibility = ScrollBarVisibility.Auto`, the content is measured with height `float.PositiveInfinity`, although the scroll bar stays collapsed in both modes. `ScrollViewer` sets the capability during its own measure, and measure work queued during the measure phase runs on a later frame: after the first `UIRoot.ProcessFrame` the content's last measure constraint is still `320`, after the second it is `float.PositiveInfinity`. Setting the current value again does not invalidate anything.

During arrange, the presenter coerces offsets into the valid range, clips rendering and hit testing to the rounded final rectangle, and arranges the content at `FinalRect.X - HorizontalOffset` and `FinalRect.Y - VerticalOffset`. For enabled axes, content is arranged at least as large as the greater of extent and viewport; for disabled axes, content is arranged to the viewport size.

Offsets must be finite, non-negative values. `SetHorizontalOffset` and `SetVerticalOffset` coerce requested values to the valid scroll range. When an axis is disabled, its offset is forced to `0`.

Changing `HorizontalOffset` or `VerticalOffset` affects arrange, render, and hit testing, but not measure. The presenter clears its clip when detached from the element tree.

`MakeVisible(UIElement visual, LayoutRect rectangle)` works as WPF's `ScrollContentPresenter.MakeVisible`, which `ScrollViewer` calls when an element raises `RequestBringIntoView`. `rectangle` is in the coordinates of `visual`, an element inside the content; `LayoutRect.Empty` means the whole `visual`. On each axis that can scroll, the offset changes by the least amount that shows the rectangle, by WPF's minimal-scroll rule:

- A rectangle above (or left of) the viewport, and not larger than it, scrolls until its start is at the viewport start.
- A rectangle below (or right of) the viewport, and not larger than it, scrolls until its end is at the viewport end.
- A rectangle larger than the viewport aligns its end when it is above, and its start when it is below.
- A rectangle inside the viewport, or one that covers the whole viewport, keeps the offset.

The method returns the part of the rectangle that is visible after the scroll, in the presenter's coordinates, or `LayoutRect.Empty` when `visual` is not inside the content or nothing of the rectangle is visible. It reads the positions of the last arrange, and an offset set since then does not change them, so a second call before the next arrange gives the same offset. For example, a 20-pixel-high element that starts 300 pixels down in the content of a presenter whose viewport is 100 high and whose `VerticalOffset` is `0`:

```csharp
LayoutRect visible = viewer.Presenter.MakeVisible(button, LayoutRect.Empty);
// viewer.Presenter.VerticalOffset is 220; visible is (0, 80, button width, 20).
```

For content elements that opt into layout motion, movement caused only by a scroll-offset change does not start or restart a layout correction. Layout motion excludes the applied scroll displacement from snapshot comparison; genuine layout changes inside the content still animate.

## Constructors
| Name | Description |
| --- | --- |
| `ScrollContentPresenter()` | Initializes a new instance of `ScrollContentPresenter`. |

## Fields
| Name | Type | Description |
| --- | --- | --- |
| `HorizontalOffsetProperty` | `UiProperty<float>` | Identifies the `HorizontalOffset` UI property. The default value is `0`; values are finite, non-negative, and coerced to the horizontal scroll range. |
| `VerticalOffsetProperty` | `UiProperty<float>` | Identifies the `VerticalOffset` UI property. The default value is `0`; values are finite, non-negative, and coerced to the vertical scroll range. |
| `ExtentWidthProperty` | `UiProperty<float>` | Identifies the `ExtentWidth` UI property. The default value is `0`; values are finite and non-negative. |
| `ExtentHeightProperty` | `UiProperty<float>` | Identifies the `ExtentHeight` UI property. The default value is `0`; values are finite and non-negative. |
| `ViewportWidthProperty` | `UiProperty<float>` | Identifies the `ViewportWidth` UI property. The default value is `0`; values are finite and non-negative. |
| `ViewportHeightProperty` | `UiProperty<float>` | Identifies the `ViewportHeight` UI property. The default value is `0`; values are finite and non-negative. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `HorizontalOffset` | `float` | Gets the current horizontal scroll offset. The setter is private; use `SetHorizontalOffset(float)`. |
| `VerticalOffset` | `float` | Gets the current vertical scroll offset. The setter is private; use `SetVerticalOffset(float)`. |
| `ExtentWidth` | `float` | Gets the measured content extent width. The setter is private and updated during layout. |
| `ExtentHeight` | `float` | Gets the measured content extent height. The setter is private and updated during layout. |
| `ViewportWidth` | `float` | Gets the current viewport width. The setter is private and updated during layout. |
| `ViewportHeight` | `float` | Gets the current viewport height. The setter is private and updated during layout. |
| `CanHorizontallyScroll` | `bool` | Gets or sets whether horizontal scrolling is enabled. The default value is `true`; when disabled, the horizontal offset is coerced to `0`. A change invalidates the presenter's measure. |
| `CanVerticallyScroll` | `bool` | Gets or sets whether vertical scrolling is enabled. The default value is `true`; when disabled, the vertical offset is coerced to `0`. A change invalidates the presenter's measure. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `SetHorizontalOffset(float offset)` | `void` | Requests a horizontal offset. The stored value is coerced to `0` when horizontal scrolling is disabled, or to the range `0` through `ExtentWidth - ViewportWidth` when enabled. |
| `SetVerticalOffset(float offset)` | `void` | Requests a vertical offset. The stored value is coerced to `0` when vertical scrolling is disabled, or to the range `0` through `ExtentHeight - ViewportHeight` when enabled. |
| `MakeVisible(UIElement visual, LayoutRect rectangle)` | `LayoutRect` | Scrolls each axis that can scroll by the least amount that shows `rectangle` of `visual` (`LayoutRect.Empty` is the whole element), and returns its visible part in this presenter's coordinates, or `LayoutRect.Empty`. |

## Events
This class does not declare additional public events.

## Property Information
| Property | Identifier field | Default value | Validation | Metadata/options |
| --- | --- | --- | --- | --- |
| `HorizontalOffset` | `HorizontalOffsetProperty` | `0` | Finite and `>= 0`; coerced to horizontal scroll range or `0` when disabled. | `AffectsArrange`, `AffectsRender`, `AffectsHitTest` |
| `VerticalOffset` | `VerticalOffsetProperty` | `0` | Finite and `>= 0`; coerced to vertical scroll range or `0` when disabled. | `AffectsArrange`, `AffectsRender`, `AffectsHitTest` |
| `ExtentWidth` | `ExtentWidthProperty` | `0` | Finite and `>= 0`. | `None` |
| `ExtentHeight` | `ExtentHeightProperty` | `0` | Finite and `>= 0`. | `None` |
| `ViewportWidth` | `ViewportWidthProperty` | `0` | Finite and `>= 0`. | `None` |
| `ViewportHeight` | `ViewportHeightProperty` | `0` | Finite and `>= 0`. | `None` |

## Applies To
Project: `Cerneala`

UI area: retained controls, scrolling, layout, clipping, rendering, and hit testing.

## See Also
- `ScrollViewer`
- `IScrollInfo`
- `ContentControl`
- `UIElement`
