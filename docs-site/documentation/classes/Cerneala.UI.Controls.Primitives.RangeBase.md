# RangeBase Class

## Definition

Namespace: `Cerneala.UI.Controls.Primitives`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Primitives/RangeBase.cs`

Provides the shared finite numeric range state used by retained controls such as sliders, scroll bars, and progress indicators.

```csharp
public class RangeBase : Control
```

Inheritance:
`Object` -> `UiObject` -> `UIElement` -> `Control` -> `RangeBase`

Derived:
`Cerneala.UI.Controls.Slider`, `Cerneala.UI.Controls.ProgressBar`, `Cerneala.UI.Controls.Primitives.ScrollBar`

## Examples

Create a range and set a value that is coerced into the active bounds:

```csharp
using Cerneala.UI.Controls.Primitives;

RangeBase range = new()
{
    Minimum = 10,
    Maximum = 20
};

range.Value = 25;
float current = range.Value; // 20
```

Changing an endpoint also coerces the current value:

```csharp
using Cerneala.UI.Controls.Primitives;

RangeBase range = new()
{
    Minimum = 0,
    Maximum = 100,
    Value = 80
};

range.Maximum = 40;
float current = range.Value; // 40
```

## Remarks

`RangeBase` is the common base for controls that expose a bounded `float` value. It stores the lower endpoint in `Minimum`, the upper endpoint in `Maximum`, the current value in `Value`, and step sizes in `SmallChange` and `LargeChange`.

`Minimum`, `Maximum`, and `Value` accept only finite floating-point values. `SmallChange` and `LargeChange` accept only finite, non-negative values.

`Value` is coerced into the current `Minimum..Maximum` range. Setting `Value` above `Maximum` stores `Maximum`; setting it below `Minimum` stores `Minimum`. When `Minimum` or `Maximum` changes, `Value` is coerced again.

The range endpoints are kept ordered. If `Minimum` is set above `Maximum`, `Maximum` is raised to match `Minimum`. If `Maximum` is set below `Minimum`, `Minimum` is lowered to match `Maximum`.

`MinimumProperty` and `MaximumProperty` affect arrange and render invalidation. `ValueProperty` affects arrange, render, and input visual invalidation. `SmallChangeProperty` and `LargeChangeProperty` affect input visual invalidation.

## Constructors

| Name | Description |
| --- | --- |
| `RangeBase()` | Initializes a new `RangeBase` with default range values from its UI property metadata. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `MinimumProperty` | `UiProperty<float>` | Identifies the `Minimum` property. The default value is `0`; values must be finite. Metadata options are `AffectsArrange` and `AffectsRender`. |
| `MaximumProperty` | `UiProperty<float>` | Identifies the `Maximum` property. The default value is `1`; values must be finite. Metadata options are `AffectsArrange` and `AffectsRender`. |
| `ValueProperty` | `UiProperty<float>` | Identifies the `Value` property. The default value is `0`; values must be finite and are coerced into the active range. Metadata options are `AffectsArrange`, `AffectsRender`, and `AffectsInputVisual`. |
| `SmallChangeProperty` | `UiProperty<float>` | Identifies the `SmallChange` property. The default value is `0.1`; values must be finite and non-negative. Metadata option is `AffectsInputVisual`. |
| `LargeChangeProperty` | `UiProperty<float>` | Identifies the `LargeChange` property. The default value is `1`; values must be finite and non-negative. Metadata option is `AffectsInputVisual`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Minimum` | `float` | Gets or sets the lower bound for `Value`. If set above `Maximum`, `Maximum` is raised to match it. |
| `Maximum` | `float` | Gets or sets the upper bound for `Value`. If set below `Minimum`, `Minimum` is lowered to match it. |
| `Value` | `float` | Gets or sets the current range value. The value is coerced into `Minimum..Maximum`. |
| `SmallChange` | `float` | Gets or sets the small range increment. Values must be finite and non-negative. |
| `LargeChange` | `float` | Gets or sets the large range increment. Values must be finite and non-negative. |

## Methods

`RangeBase` does not declare public methods beyond inherited members.

## Protected Methods

| Name | Description |
| --- | --- |
| `CoerceToRange(float value)` | Returns `value` clamped into the current `Minimum..Maximum` range. |
| `OnPropertyChanged(UiPropertyChangedEventArgs args)` | Handles range property changes by preserving endpoint order and coercing `Value` when needed. |

## Events

`RangeBase` does not declare public events.

## Property Information

| Property | Identifier field | Default value | Validation | Metadata/options |
| --- | --- | --- | --- | --- |
| `Minimum` | `MinimumProperty` | `0` | Finite `float` | `UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender` |
| `Maximum` | `MaximumProperty` | `1` | Finite `float` | `UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender` |
| `Value` | `ValueProperty` | `0` | Finite `float`; coerced to range | `UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual` |
| `SmallChange` | `SmallChangeProperty` | `0.1f` | Finite, non-negative `float` | `UiPropertyOptions.AffectsInputVisual` |
| `LargeChange` | `LargeChangeProperty` | `1` | Finite, non-negative `float` | `UiPropertyOptions.AffectsInputVisual` |

## Range Coercion

| Change | Result |
| --- | --- |
| `Value` is set below `Minimum` | `Value` becomes `Minimum`. |
| `Value` is set above `Maximum` | `Value` becomes `Maximum`. |
| `Minimum` is set above `Maximum` | `Maximum` is set to the new `Minimum`, then `Value` is coerced. |
| `Maximum` is set below `Minimum` | `Minimum` is set to the new `Maximum`, then `Value` is coerced. |
| `Value` is cleared while its default is outside the current range | The effective `Value` is coerced back into the current range. |

## Applies To

Cerneala retained UI controls that need bounded numeric state.

## See Also

- `Cerneala.UI.Controls.Slider`
- `Cerneala.UI.Controls.ProgressBar`
- `Cerneala.UI.Controls.Primitives.ScrollBar`
- `Cerneala.UI.Controls.Control`
