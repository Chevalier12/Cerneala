# ScrollBar Class

## Definition

Namespace: `Cerneala.UI.Controls.Primitives`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Primitives/ScrollBar.cs`

Represents a retained scroll bar control that synchronizes inherited range values with an owned `Track`.

```csharp
public class ScrollBar : RangeBase
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `RangeBase` -> `ScrollBar`

## Examples

Create a horizontal scroll bar with a viewport-sized thumb:

```csharp
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Layout.Panels;

ScrollBar scrollBar = new()
{
    Orientation = Orientation.Horizontal,
    Minimum = 0,
    Maximum = 100,
    Value = 25,
    ViewportSize = 20
};

float current = scrollBar.Track.Value; // 25
```

Create a vertical scroll bar and update its value programmatically:

```csharp
using Cerneala.UI.Controls.Primitives;

ScrollBar scrollBar = new()
{
    Minimum = 0,
    Maximum = 500,
    ViewportSize = 120
};

scrollBar.Value = 80;
float trackValue = scrollBar.Track.Value; // 80
```

## Remarks

`ScrollBar` derives from `RangeBase`, so `Minimum`, `Maximum`, `Value`, `SmallChange`, and `LargeChange` use the inherited range validation and coercion rules. `Value` is coerced into the active `Minimum..Maximum` range, and non-finite range values are rejected by the inherited UI properties.

The constructor creates one fallback `Track`, subscribes to its `ValueChanged` event, adds it as both a logical and visual child, and sets default visuals: `Background` is `DrawColor(235, 235, 235)`, `BorderColor` is `DrawColor(130, 130, 130)`, and `BorderThickness` is `new Thickness(1)`.

`Orientation` defaults to `Orientation.Vertical`. When no template child is present, measuring a horizontal scroll bar returns a width of at least `32` and a height of `12`; measuring a vertical scroll bar returns a width of `12` and a height of at least `32`. Arrange gives the fallback track the scroll bar final rectangle.

The scroll bar keeps its track synchronized with `Minimum`, `Maximum`, `Value`, `SmallChange`, `LargeChange`, `ViewportSize`, and `Orientation`. When the track value changes, for example through thumb dragging or track input handled by `Track`, the scroll bar copies `Track.Value` back into `Value`.

Setting the inherited `Template` property removes the fallback track from the scroll bar logical and visual children and lets the base template path handle layout. Clearing `Template` adds the fallback track back. The `Track` property still returns the same track instance.

The default renderer fills the scroll bar background when no template child is present. The fallback renderer does not draw the border itself; the owned `Track` handles its own background and border rendering.

## Constructors

| Name | Description |
| --- | --- |
| `ScrollBar()` | Initializes a new `ScrollBar`, creates its fallback `Track`, subscribes to track value changes, adds the track as a child while no classic template is applied, and assigns default background, border color, border thickness, orientation, and viewport size values. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `OrientationProperty` | `UiProperty<Orientation>` | Identifies the `Orientation` UI property. The default value is `Orientation.Vertical`; metadata options are `AffectsMeasure`, `AffectsArrange`, and `AffectsRender`. |
| `ViewportSizeProperty` | `UiProperty<float>` | Identifies the `ViewportSize` UI property. The default value is `0`; values must be finite and non-negative. Metadata options are `AffectsArrange` and `AffectsRender`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Orientation` | `Orientation` | Gets or sets whether the scroll bar and its fallback track are laid out vertically or horizontally. |
| `ViewportSize` | `float` | Gets or sets the visible viewport size forwarded to `Track.ViewportSize`, affecting the fallback track thumb length. |
| `Track` | `Track` | Gets the fallback track created by the constructor. The scroll bar synchronizes this track with its range values, viewport size, and orientation. |

## Methods

`ScrollBar` does not declare public methods beyond inherited members.

## Events

`ScrollBar` does not declare public events.

## Relevant Inherited Fields

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `MinimumProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `Minimum` property. Default is `0`; values must be finite. |
| `MaximumProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `Maximum` property. Default is `1`; values must be finite. |
| `ValueProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `Value` property. Default is `0`; values must be finite and are coerced to the active range. |
| `SmallChangeProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `SmallChange` property. Default is `0.1`; values must be finite and non-negative. |
| `LargeChangeProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `LargeChange` property. Default is `1`; values must be finite and non-negative. |
| `TemplateProperty` | `UiProperty<ControlTemplate?>` | `Control` | Identifies the classic control template property. `ScrollBar` removes or restores its fallback track when this property changes. |

## Relevant Inherited Properties

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `Minimum` | `float` | `RangeBase` | Gets or sets the lower bound for `Value`. If set above `Maximum`, `Maximum` is raised to match it. |
| `Maximum` | `float` | `RangeBase` | Gets or sets the upper bound for `Value`. If set below `Minimum`, `Minimum` is lowered to match it. |
| `Value` | `float` | `RangeBase` | Gets or sets the current scroll bar value. The value is coerced into the current range and synchronized to `Track.Value`. |
| `SmallChange` | `float` | `RangeBase` | Gets or sets the small range increment synchronized to `Track.SmallChange`. |
| `LargeChange` | `float` | `RangeBase` | Gets or sets the large range increment synchronized to `Track.LargeChange`. |
| `Background` | `DrawColor` | `Control` | Gets or sets the fallback scroll bar background fill. The constructor sets it to `DrawColor(235, 235, 235)`. |
| `BorderColor` | `DrawColor` | `Control` | Gets or sets the inherited border color. The constructor sets it to `DrawColor(130, 130, 130)`. |
| `BorderThickness` | `Thickness` | `Control` | Gets or sets the inherited border thickness. The constructor sets it to `new Thickness(1)`. |
| `Template` | `ControlTemplate?` | `Control` | Gets or sets the classic control template. When present, the fallback track is removed from the scroll bar child collections. |

## Property Information

| Property | Identifier field | Default value | Metadata/options |
| --- | --- | --- | --- |
| `Orientation` | `OrientationProperty` | `Orientation.Vertical` | `UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender` |
| `ViewportSize` | `ViewportSizeProperty` | `0` | `UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender`; value must be finite and non-negative. |

## Layout Behavior

| Condition | Result |
| --- | --- |
| `TemplateChild` is `null` during measure and `Orientation` is `Horizontal` | Synchronizes `Track`, measures it, and returns `new LayoutSize(MathF.Max(32, Track.DesiredSize.Width), 12)`. |
| `TemplateChild` is `null` during measure and `Orientation` is `Vertical` | Synchronizes `Track`, measures it, and returns `new LayoutSize(12, MathF.Max(32, Track.DesiredSize.Height))`. |
| `TemplateChild` is `null` during arrange | Synchronizes `Track`, arranges it with the scroll bar final rectangle, and returns that final rectangle. |
| `TemplateChild` is not `null` | Uses the base `Control` template layout path. |

## Track Synchronization

| ScrollBar member | Synchronized track member |
| --- | --- |
| `Minimum` | `Track.Minimum` |
| `Maximum` | `Track.Maximum` |
| `Value` | `Track.Value` |
| `SmallChange` | `Track.SmallChange` |
| `LargeChange` | `Track.LargeChange` |
| `ViewportSize` | `Track.ViewportSize` |
| `Orientation` | `Track.Orientation` |

## Applies to

`Cerneala.UI.Controls.Primitives.ScrollBar` in the `Cerneala` project.

## See also

- `Cerneala.UI.Controls.Primitives.RangeBase`
- `Cerneala.UI.Controls.Primitives.Track`
- `Cerneala.UI.Controls.Primitives.Thumb`
- `Cerneala.UI.Controls.ScrollViewer`
- `Cerneala.UI.Controls.Control`
