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

During measure, scroll-enabled axes are measured with an infinite available size so content can report its natural extent. Disabled axes are measured with the available viewport dimension. The measured content size becomes `ExtentWidth` and `ExtentHeight`; the finite available size becomes `ViewportWidth` and `ViewportHeight`.

During arrange, the presenter coerces offsets into the valid range, clips rendering and hit testing to the rounded final rectangle, and arranges the content at `FinalRect.X - HorizontalOffset` and `FinalRect.Y - VerticalOffset`. For enabled axes, content is arranged at least as large as the greater of extent and viewport; for disabled axes, content is arranged to the viewport size.

Offsets must be finite, non-negative values. `SetHorizontalOffset` and `SetVerticalOffset` coerce requested values to the valid scroll range. When an axis is disabled, its offset is forced to `0`.

Changing `HorizontalOffset` or `VerticalOffset` affects arrange, render, and hit testing, but not measure. The presenter clears its clip when detached from the element tree.

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
| `CanHorizontallyScroll` | `bool` | Gets or sets whether horizontal scrolling is enabled. The default value is `true`; when disabled, the horizontal offset is coerced to `0`. |
| `CanVerticallyScroll` | `bool` | Gets or sets whether vertical scrolling is enabled. The default value is `true`; when disabled, the vertical offset is coerced to `0`. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `SetHorizontalOffset(float offset)` | `void` | Requests a horizontal offset. The stored value is coerced to `0` when horizontal scrolling is disabled, or to the range `0` through `ExtentWidth - ViewportWidth` when enabled. |
| `SetVerticalOffset(float offset)` | `void` | Requests a vertical offset. The stored value is coerced to `0` when vertical scrolling is disabled, or to the range `0` through `ExtentHeight - ViewportHeight` when enabled. |

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
