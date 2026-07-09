# UiPropertyMetadata<T> Class

## Definition
Namespace: `Cerneala.UI.Core`

Assembly/Project: `Cerneala`

Source: `UI/Core/UiPropertyMetadata{T}.cs`

Stores default value, invalidation options, equality, validation, and coercion callbacks for a typed UI property.

```csharp
public sealed class UiPropertyMetadata<T>
```

## Examples

```csharp
using Cerneala.UI.Core;

UiPropertyMetadata<float> opacityMetadata = new(
    1,
    UiPropertyOptions.AffectsRender,
    validateValue: value => float.IsFinite(value) && value >= 0 && value <= 1);

UiProperty<float> opacityProperty = UiProperty<float>.Register(
    "Opacity",
    typeof(MyElement),
    opacityMetadata);
```

## Remarks

`UiPropertyMetadata<T>` is supplied when registering a `UiProperty<T>`. The property system uses it to provide the default value, decide which UI subsystems should be invalidated, compare effective values, reject invalid values, and optionally coerce assigned values.

When no equality comparer is supplied, the constructor uses `EqualityComparer<T>.Default`. `ValidateValue` and `CoerceValue` are optional; leave them `null` when the property accepts all values assignable to `T` and does not need value normalization.

## Constructors

| Name | Description |
| --- | --- |
| `UiPropertyMetadata(T, UiPropertyOptions, IEqualityComparer<T>?, ValidateValue<T>?, CoerceValue<T>?)` | Initializes metadata for a typed UI property. |

## Properties

| Name | Description |
| --- | --- |
| `DefaultValue` | Gets the value returned by the property system when no local or inherited value is present. |
| `Options` | Gets the invalidation and behavior flags associated with the property. |
| `EqualityComparer` | Gets the comparer used to determine whether effective values changed. |
| `ValidateValue` | Gets the optional validation callback used to reject invalid values. |
| `CoerceValue` | Gets the optional callback used to normalize a value before comparison and storage. |

## Applies to

Cerneala retained UI property system.

## See also

- `Cerneala.UI.Core.UiProperty<T>`
- `Cerneala.UI.Core.UiPropertyOptions`
- `Cerneala.UI.Core.ValidateValue<T>`
- `Cerneala.UI.Core.CoerceValue<T>`
