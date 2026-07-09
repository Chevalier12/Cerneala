# UiProperty<T> Class

## Definition
Namespace: `Cerneala.UI.Core`

Assembly/Project: `Cerneala`

Source: `UI/Core/UiProperty{T}.cs`

Represents a typed UI property descriptor with metadata, default value, validation, coercion, and equality behavior.

```csharp
public sealed class UiProperty<T> : UiProperty
```

Inheritance:
`object` -> `UiProperty` -> `UiProperty<T>`

## Examples

```csharp
using Cerneala.UI.Core;

UiProperty<int> countProperty = UiProperty<int>.Register(
    "Count",
    typeof(MyOwner),
    new UiPropertyMetadata<int>(
        0,
        UiPropertyOptions.AffectsRender,
        validateValue: value => value >= 0));

UiObject owner = new();
owner.SetValue(countProperty, 3);
int value = owner.GetValue(countProperty);
```

## Remarks

`UiProperty<T>` is the typed form of `UiProperty`. It stores `UiPropertyMetadata<T>`, exposes the metadata default value to the property store, and delegates equality, coercion, and validation to metadata callbacks.

`Register` creates a writable property through `UiPropertyRegistry`. `RegisterReadOnly` creates a read-only property key through the same registry.

When an untyped value is coerced or validated, the value must be assignable to `T`. `null` is accepted only when `T` can have a `null` default value. Invalid type assignments and failed validation callbacks throw `ArgumentException`.

## Properties

| Name | Description |
| --- | --- |
| `Metadata` | Gets the typed metadata for this UI property. |

## Methods

| Name | Description |
| --- | --- |
| `Register(string, Type, UiPropertyMetadata<T>)` | Registers a writable typed UI property. |
| `RegisterReadOnly(string, Type, UiPropertyMetadata<T>)` | Registers a read-only typed UI property and returns its key. |

## Applies to

Cerneala retained UI property system.

## See also

- `Cerneala.UI.Core.UiProperty`
- `Cerneala.UI.Core.UiPropertyMetadata<T>`
- `Cerneala.UI.Core.UiPropertyRegistry`
- `Cerneala.UI.Core.UiObject`
