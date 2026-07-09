# Slider Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Slider.cs`

Represents a retained range selector that exposes a draggable `Track` for changing `Value`.

```csharp
public class Slider : RangeBase
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `RangeBase` -> `Slider`

## Examples

Create a horizontal slider whose value can move from `0` to `100`:

```csharp
using Cerneala.UI.Controls;

Slider slider = new()
{
    Minimum = 0,
    Maximum = 100,
    Value = 25
};

slider.Value = 50;
float current = slider.Track.Value; // 50
```

Create a vertical slider:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Layout.Panels;

Slider slider = new()
{
    Orientation = Orientation.Vertical,
    Minimum = 0,
    Maximum = 100,
    Value = 50
};
```

## Remarks

`Slider` derives from `RangeBase`, so `Minimum`, `Maximum`, `Value`, `SmallChange`, and `LargeChange` use the same validation and range coercion as other range controls. `Value` is coerced into the active `Minimum..Maximum` range, and non-finite range values are rejected by the inherited UI properties.

The constructor creates one `Track`, subscribes to its `ValueChanged` event, and adds it as the fallback logical and visual child. When the slider has no classic `Template`, layout is delegated directly to that track: measure returns the track desired size, and arrange gives the track the slider final rectangle.

The slider keeps its track synchronized with `Minimum`, `Maximum`, `Value`, `SmallChange`, `LargeChange`, and `Orientation`. When the track value changes, for example through thumb dragging or track input handled by `Track`, the slider copies `Track.Value` back into `Value`.

Setting the inherited `Template` property removes the fallback track from the slider logical and visual children and lets the base template path handle layout. Clearing `Template` adds the fallback track back. The `Track` property still returns the same track instance.

`OrientationProperty` is registered with `UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender`, so changing `Orientation` participates in layout and render invalidation.

## Constructors

| Name | Description |
| --- | --- |
| `Slider()` | Initializes a new `Slider`, creates its fallback `Track`, subscribes to track value changes, and adds the track as a child while no classic template is applied. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `OrientationProperty` | `UiProperty<Orientation>` | Identifies the `Orientation` UI property. The default value is `Orientation.Horizontal`; metadata options are `AffectsMeasure`, `AffectsArrange`, and `AffectsRender`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Orientation` | `Orientation` | Gets or sets whether the slider track is laid out horizontally or vertically. |
| `Track` | `Track` | Gets the fallback track created by the slider constructor. The slider synchronizes this track with its range values and orientation. |

## Methods

`Slider` does not declare public methods beyond inherited members.

## Events

`Slider` does not declare public events.

## Relevant Inherited Fields

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `MinimumProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `Minimum` property. Default is `0`; values must be finite. |
| `MaximumProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `Maximum` property. Default is `1`; values must be finite. |
| `ValueProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `Value` property. Default is `0`; values must be finite and are coerced to the active range. |
| `SmallChangeProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `SmallChange` property. Default is `0.1`; values must be finite and non-negative. |
| `LargeChangeProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `LargeChange` property. Default is `1`; values must be finite and non-negative. |
| `TemplateProperty` | `UiProperty<ControlTemplate?>` | `Control` | Identifies the classic control template property. `Slider` removes or restores its fallback track when this property changes. |

## Relevant Inherited Properties

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `Minimum` | `float` | `RangeBase` | Gets or sets the lower bound for `Value`. If set above `Maximum`, `Maximum` is raised to match it. |
| `Maximum` | `float` | `RangeBase` | Gets or sets the upper bound for `Value`. If set below `Minimum`, `Minimum` is lowered to match it. |
| `Value` | `float` | `RangeBase` | Gets or sets the current slider value. The value is coerced into the current range. |
| `SmallChange` | `float` | `RangeBase` | Gets or sets the small range increment synchronized to `Track.SmallChange`. |
| `LargeChange` | `float` | `RangeBase` | Gets or sets the large range increment synchronized to `Track.LargeChange`. |
| `Template` | `ControlTemplate?` | `Control` | Gets or sets the classic control template. When present, the fallback track is removed from the slider child collections. |

## Property Information

| Item | Value |
| --- | --- |
| Identifier field | `OrientationProperty` |
| Property type | `Orientation` |
| Default value | `Orientation.Horizontal` |
| Metadata/options | `UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender` |

## Layout Behavior

| Condition | Result |
| --- | --- |
| `TemplateChild` is `null` during measure | Synchronizes `Track`, measures it with the available size, and returns `Track.DesiredSize`. |
| `TemplateChild` is `null` during arrange | Synchronizes `Track`, arranges it with the slider final rectangle, and returns that final rectangle. |
| `TemplateChild` is not `null` | Uses the base `Control` template layout path. |
| `Orientation` is `Orientation.Horizontal` | The synchronized track lays out horizontally. |
| `Orientation` is `Orientation.Vertical` | The synchronized track lays out vertically. |

## Track Synchronization

| Slider member | Synchronized track member |
| --- | --- |
| `Minimum` | `Track.Minimum` |
| `Maximum` | `Track.Maximum` |
| `Value` | `Track.Value` |
| `SmallChange` | `Track.SmallChange` |
| `LargeChange` | `Track.LargeChange` |
| `Orientation` | `Track.Orientation` |

## Applies to

`Cerneala.UI.Controls.Slider` in the `Cerneala` project.

## See also

- `Cerneala.UI.Controls.Primitives.RangeBase`
- `Cerneala.UI.Controls.Primitives.Track`
- `Cerneala.UI.Controls.Primitives.Thumb`
- `Cerneala.UI.Controls.Control`
