# UiPropertyRegistry Class

## Definition
Namespace: `Cerneala.UI.Core`

Assembly/Project: `Cerneala`

Source: `UI/Core/UiPropertyRegistry.cs`

Provides the process-wide registry used to create, identify, and query `UiProperty` definitions.

```csharp
public static class UiPropertyRegistry
```

Inheritance:
`object` -> `UiPropertyRegistry`

## Examples
Register a normal UI property and then query properties that affect rendering.

```csharp
using Cerneala.UI.Core;

public sealed class MeterElement : UiObject
{
    public static readonly UiProperty<double> ValueProperty =
        UiPropertyRegistry.Register(
            "Value",
            typeof(MeterElement),
            new UiPropertyMetadata<double>(
                0.0,
                UiPropertyOptions.AffectsRender,
                validateValue: value => value >= 0.0 && value <= 1.0));
}

IReadOnlyList<UiProperty> renderProperties =
    UiPropertyRegistry.GetPropertiesWithOptions(UiPropertyOptions.AffectsRender);
```

Register a read-only UI property and keep the returned key for owner-side mutation paths.

```csharp
using Cerneala.UI.Core;

public sealed class PressableElement : UiObject
{
    public static readonly UiPropertyKey<bool> IsPressedPropertyKey =
        UiPropertyRegistry.RegisterReadOnly(
            "IsPressed",
            typeof(PressableElement),
            new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsInputVisual));

    public static readonly UiProperty<bool> IsPressedProperty =
        IsPressedPropertyKey.Property;
}
```

## Remarks
`UiPropertyRegistry` stores `UiProperty` instances in a static registry keyed by owner type and property name. Registration assigns each property a unique, increasing `Id`, and query methods return properties ordered by that `Id`.

The registry rejects duplicate registrations for the same `(ownerType, name)` pair. Names must be non-empty and non-whitespace because `UiProperty` validates the name during construction. `Register` also rejects a null `ownerType` or null metadata. `RegisterReadOnly` creates new metadata with the `ReadOnly` option added while preserving the original default value, equality comparer, validation callback, and coercion callback.

The registry is backed by a `ConcurrentDictionary`, but it is append-only from the public API: this class does not expose unregister or clear operations. Tests and samples that register properties should use unique names when they share the same process.

`GetPropertiesWithOptions` returns properties whose `Options` contain all requested flags. Passing `UiPropertyOptions.None` matches all registered properties because every flags value contains `None`.

## Methods
| Name | Description |
| --- | --- |
| `Register<T>(string name, Type ownerType, UiPropertyMetadata<T> metadata)` | Creates and registers a writable `UiProperty<T>` for the given owner type and name. Throws `InvalidOperationException` when the same owner type and name are already registered. |
| `RegisterReadOnly<T>(string name, Type ownerType, UiPropertyMetadata<T> metadata)` | Creates and registers a read-only `UiProperty<T>` and returns its `UiPropertyKey<T>`. The registered property's metadata always includes `UiPropertyOptions.ReadOnly`. |
| `GetRegisteredProperties()` | Returns a snapshot of all registered properties ordered by `UiProperty.Id`. |
| `GetPropertiesWithOptions(UiPropertyOptions options)` | Returns a snapshot of registered properties whose options contain all requested flags, ordered by `UiProperty.Id`. |

## Applies to
Project: `Cerneala`

UI property system types in `Cerneala.UI.Core`.

## See also
- `UiProperty`
- `UiProperty<T>`
- `UiPropertyKey<T>`
- `UiPropertyMetadata<T>`
- `UiPropertyOptions`
