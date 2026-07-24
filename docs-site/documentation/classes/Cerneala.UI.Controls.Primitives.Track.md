# Track Class

## Definition
Namespace: `Cerneala.UI.Controls.Primitives`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Primitives/Track.cs`

Represents a retained range track that positions and drags a `Thumb` over a finite numeric range.

```csharp
[TemplatePart("PART_Thumb", typeof(Thumb))]
public class Track : Control
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `Track`

## Examples

Create and arrange a horizontal track:

```csharp
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

Track track = new()
{
    Minimum = 0,
    Maximum = 100,
    Value = 50,
    Orientation = Orientation.Horizontal
};

track.Measure(new MeasureContext(new LayoutSize(100, 20)));
track.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 20)));

LayoutRect thumbBounds = track.Thumb.ArrangedBounds; // X = 50, Width = 10
```

Use `ViewportSize` to size the thumb for a scrollable viewport:

```csharp
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

Track track = new()
{
    Minimum = 0,
    Maximum = 100,
    ViewportSize = 25,
    Orientation = Orientation.Horizontal
};

track.Measure(new MeasureContext(new LayoutSize(100, 20)));
track.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 20)));
```

## Remarks

`Track` requires a `Thumb` named `PART_Thumb` in its active component template. The default template supplies that part through `TrackLayoutPanel`, which template authors can also use to keep thumb arrangement owned by `Track`. `Thumb` always returns the part from the active template rather than a parallel fallback instance. Replacing the template unsubscribes the old thumb before subscribing to the new one. Assigning `null` to `ComponentTemplate` clears the local override and restores the default template.

`Minimum`, `Maximum`, and `Value` accept only finite floating-point values. `ViewportSize`, `SmallChange`, and `LargeChange` accept only finite, non-negative values. `Value` is coerced into the active `Minimum..Maximum` range. If an endpoint change would invert the range, the other endpoint is moved to preserve an ordered range, then `Value` is coerced again.

The default template layout is orientation-aware. A horizontal track reports a desired size of at least `32` by the thumb height or `10`; a vertical track reports the thumb width or `10` by at least `32`. The track control remains the single owner of value geometry and arranges the active thumb along the arranged width or height. Without a viewport size, thumb length is `MathF.Min(trackLength, 10)`. With a positive `ViewportSize`, thumb length is proportional to `ViewportSize / (Range + ViewportSize)`, clamped to at least `10` and at most the full track length. If the range is zero, the thumb fills the full track length.

Dragging the thumb updates `Value` from the pointer delta, using horizontal movement for horizontal tracks and vertical movement for vertical tracks. `ValueChanged` is raised when the stored value actually changes. A left mouse down on the track outside the thumb compares the clicked position with the current value and applies `DecreaseLarge` or `IncreaseLarge`; the input event is marked handled only when this changes the value.

The default template binds `Background`, `BorderBrush`, and `BorderThickness` to its border root. The constructor initializes `Background` to `new SolidColorBrush(new Color(225, 225, 225))`, `BorderBrush` to `new SolidColorBrush(new Color(120, 120, 120))`, and `BorderThickness` to `new Thickness(1)` at the `AspectBase` value source so markup aspects can replace those visual defaults. It also initializes `SmallChange` to `0.1f` and `LargeChange` to `1`.

`Slider` and `ScrollBar` use `Track` as their owned range interaction primitive and synchronize their range state into it.

## Constructors

| Name | Description |
| --- | --- |
| `Track()` | Initializes the default visuals and change increments, registers left-mouse-down handling, and applies the default component template containing `PART_Thumb`. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `MinimumProperty` | `UiProperty<float>` | Identifies the `Minimum` property. The default value is `0`; values must be finite. Metadata options are `AffectsArrange` and `AffectsRender`. |
| `MaximumProperty` | `UiProperty<float>` | Identifies the `Maximum` property. The default value is `1`; values must be finite. Metadata options are `AffectsArrange` and `AffectsRender`. |
| `ValueProperty` | `UiProperty<float>` | Identifies the `Value` property. The default value is `0`; values must be finite and are coerced into the active range. Metadata options are `AffectsArrange`, `AffectsRender`, and `AffectsInputVisual`. |
| `ViewportSizeProperty` | `UiProperty<float>` | Identifies the `ViewportSize` property. The default value is `0`; values must be finite and non-negative. Metadata options are `AffectsArrange` and `AffectsRender`. |
| `SmallChangeProperty` | `UiProperty<float>` | Identifies the `SmallChange` property. The default value is `0.1`; values must be finite and non-negative. Metadata option is `AffectsInputVisual`. |
| `LargeChangeProperty` | `UiProperty<float>` | Identifies the `LargeChange` property. The default value is `1`; values must be finite and non-negative. Metadata option is `AffectsInputVisual`. |
| `OrientationProperty` | `UiProperty<Orientation>` | Identifies the `Orientation` property. The default value is `Orientation.Horizontal`; metadata options are `AffectsMeasure`, `AffectsArrange`, and `AffectsRender`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Thumb` | `Thumb` | Gets the `PART_Thumb` instance from the active component template. The getter applies the template before returning. |
| `Minimum` | `float` | Gets or sets the lower bound for `Value`. If set above `Maximum`, `Maximum` is raised to match it. |
| `Maximum` | `float` | Gets or sets the upper bound for `Value`. If set below `Minimum`, `Minimum` is lowered to match it. |
| `Value` | `float` | Gets or sets the current range value. The value is coerced into `Minimum..Maximum`. |
| `ViewportSize` | `float` | Gets or sets the visible viewport size used to compute thumb length. Values must be finite and non-negative. |
| `SmallChange` | `float` | Gets or sets the small range increment used by `DecreaseSmall` and `IncreaseSmall`. Values must be finite and non-negative. |
| `LargeChange` | `float` | Gets or sets the large range increment used by `DecreaseLarge`, `IncreaseLarge`, and page-like track clicks. Values must be finite and non-negative. |
| `Orientation` | `Orientation` | Gets or sets whether the track lays out horizontally or vertically. |
| `ValueRatio` | `float` | Gets the current `Value` normalized into the active range as `0..1`; returns `0` when the range is zero. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `DecreaseLarge()` | `void` | Subtracts `LargeChange` from `Value`; the result is coerced into the active range. |
| `IncreaseLarge()` | `void` | Adds `LargeChange` to `Value`; the result is coerced into the active range. |
| `DecreaseSmall()` | `void` | Subtracts `SmallChange` from `Value`; the result is coerced into the active range. |
| `IncreaseSmall()` | `void` | Adds `SmallChange` to `Value`; the result is coerced into the active range. |
| `ValueFromPoint(float x, float y)` | `float` | Converts an arranged point to a range value by centering the thumb over the point, clamping the result to the track travel area, and mapping it into `Minimum..Maximum`. |

## Events

| Name | Event Type | Description |
| --- | --- | --- |
| `ValueChanged` | `EventHandler?` | Raised when `Value` changes through property updates or thumb dragging. Thumb dragging raises it only when the clamped value changed. |

## Property Information

| Property | Identifier field | Default value | Validation | Metadata/options |
| --- | --- | --- | --- | --- |
| `Minimum` | `MinimumProperty` | `0` | Finite `float` | `UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender` |
| `Maximum` | `MaximumProperty` | `1` | Finite `float` | `UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender` |
| `Value` | `ValueProperty` | `0` | Finite `float`; coerced to range | `UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual` |
| `ViewportSize` | `ViewportSizeProperty` | `0` | Finite, non-negative `float` | `UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender` |
| `SmallChange` | `SmallChangeProperty` | `0.1f` | Finite, non-negative `float` | `UiPropertyOptions.AffectsInputVisual` |
| `LargeChange` | `LargeChangeProperty` | `1` | Finite, non-negative `float` | `UiPropertyOptions.AffectsInputVisual` |
| `Orientation` | `OrientationProperty` | `Orientation.Horizontal` | `Orientation` value | `UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender` |

## Range Behavior

| Change | Result |
| --- | --- |
| `Value` is set below `Minimum` | `Value` becomes `Minimum`. |
| `Value` is set above `Maximum` | `Value` becomes `Maximum`. |
| `Minimum` is set above `Maximum` | `Maximum` is set to the new `Minimum`, then `Value` is coerced. |
| `Maximum` is set below `Minimum` | `Minimum` is set to the new `Maximum`, then `Value` is coerced. |
| `Minimum` equals `Maximum` | `ValueRatio` returns `0`, and a positive `ViewportSize` makes the thumb fill the track length. |

## Layout Behavior

| Condition | Result |
| --- | --- |
| Default template and `Orientation.Horizontal` | The internal panel reports at least `32` width and at least the active thumb height or `10`. |
| Default template and `Orientation.Vertical` | The internal panel reports at least the active thumb width or `10` and at least `32` height. |
| Any valid template | `Track` arranges the active `PART_Thumb` at the offset implied by `ValueRatio` and the computed thumb length. |
| Track length is less than the minimum thumb length | The thumb is clamped to the track length and has zero travel. |

## Relevant Inherited Properties

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `Background` | `Brush?` | `Control` | Gets or sets the fill brush bound to the default template root. The constructor sets a solid brush with color `Color(225, 225, 225)`. |
| `BorderBrush` | `Brush?` | `Control` | Gets or sets the border brush bound to the default template root. The constructor sets a solid brush with color `Color(120, 120, 120)`. |
| `BorderThickness` | `Thickness` | `Control` | Gets or sets the border thickness bound to the default template root. The constructor sets it to `new Thickness(1)`. |
| `ComponentTemplate` | `ComponentTemplate?` | `Control` | Gets or sets the control template. Valid templates must register `PART_Thumb` as a `Thumb`; assigning `null` restores the default template. |

## Applies to

`Cerneala.UI.Controls.Primitives.Track` in the `Cerneala` project.

## See also

- `Cerneala.UI.Controls.Primitives.Thumb`
- `Cerneala.UI.Controls.Primitives.TrackLayoutPanel`
- `Cerneala.UI.Controls.Primitives.ScrollBar`
- `Cerneala.UI.Controls.Slider`
- `Cerneala.UI.Controls.Control`
