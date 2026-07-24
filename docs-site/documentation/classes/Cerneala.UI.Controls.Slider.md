# Slider Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Slider.cs`

Represents a retained range selector that exposes a draggable `Track` for changing `Value`.

```csharp
[TemplatePart("PART_Track", typeof(Track))]
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

The constructor installs the default component template at the aspect-base value source. The default template supplies the required `PART_Track` part.

The slider keeps its track synchronized with `Minimum`, `Maximum`, `Value`, `SmallChange`, `LargeChange`, and `Orientation`. Dragging the thumb changes the value continuously, while clicking outside the thumb moves the value directly to the pointer position. When the track value changes, the slider copies `Track.Value` back into `Value`.

Custom templates must register a `Track` named `PART_Track`. The `Track` property applies the current template and returns that active part. Replacing a template detaches the previous track event subscription before synchronizing the new part. Clearing a locally assigned template restores the default aspect-base template.

`OrientationProperty` is registered with `UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender`, so changing `Orientation` participates in layout and render invalidation.

## Constructors

| Name | Description |
| --- | --- |
| `Slider()` | Initializes a new `Slider` with its default component template. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `OrientationProperty` | `UiProperty<Orientation>` | Identifies the `Orientation` UI property. The default value is `Orientation.Horizontal`; metadata options are `AffectsMeasure`, `AffectsArrange`, and `AffectsRender`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Orientation` | `Orientation` | Gets or sets whether the slider track is laid out horizontally or vertically. |
| `Track` | `Track` | Applies the current template and gets its required `PART_Track` part. |

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
| `ComponentTemplateProperty` | `UiProperty<ComponentTemplate?>` | `Control` | Identifies the control template property. Slider templates must provide `PART_Track`. |

## Relevant Inherited Properties

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `Minimum` | `float` | `RangeBase` | Gets or sets the lower bound for `Value`. If set above `Maximum`, `Maximum` is raised to match it. |
| `Maximum` | `float` | `RangeBase` | Gets or sets the upper bound for `Value`. If set below `Minimum`, `Minimum` is lowered to match it. |
| `Value` | `float` | `RangeBase` | Gets or sets the current slider value. The value is coerced into the current range. |
| `SmallChange` | `float` | `RangeBase` | Gets or sets the small range increment synchronized to `Track.SmallChange`. |
| `LargeChange` | `float` | `RangeBase` | Gets or sets the large range increment synchronized to `Track.LargeChange`. |
| `ComponentTemplate` | `ComponentTemplate?` | `Control` | Gets or sets the control template. A custom template must register a `Track` named `PART_Track`. |

## Property Information

| Item | Value |
| --- | --- |
| Identifier field | `OrientationProperty` |
| Property type | `Orientation` |
| Default value | `Orientation.Horizontal` |
| Metadata/options | `UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender` |

## Template Parts

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `PART_Track` | `Track` | Yes | Owns thumb layout and pointer input for the slider value. |

## Layout Behavior

The slider applies its component template before measure, synchronizes `PART_Track`, and uses the standard `Control` template layout path. It synchronizes the track again before arrange. Horizontal and vertical layout follow the `Orientation` value copied to the active track.

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
