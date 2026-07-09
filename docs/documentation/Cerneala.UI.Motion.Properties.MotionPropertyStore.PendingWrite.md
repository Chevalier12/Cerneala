# MotionPropertyStore.PendingWrite Record

## Definition

Namespace: `Cerneala.UI.Motion.Properties`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Properties/MotionPropertyStore.cs`

Represents one staged animation-source property operation waiting to be flushed by `MotionPropertyStore`.

```csharp
private readonly record struct PendingWrite(
    PendingWriteKind Kind,
    UiObject Target,
    UiProperty Property,
    object? Value,
    MotionPropertyInvalidationCategory Category)
```

Containing type:
`MotionPropertyStore`

## Examples

`PendingWrite` is private to `MotionPropertyStore`; callers stage writes through motion bindings rather than constructing it directly. The store creates set and clear writes internally:

```csharp
MotionPropertyKey key = new(target, property);
writes[key] = PendingWrite.Set(target, property, value, category);

writes[key] = PendingWrite.Clear(target, property, category);
```

## Remarks

`PendingWrite` is an implementation detail used by `MotionPropertyStore` to batch animation writes by `MotionPropertyKey`. Staging replaces any previous write for the same target/property pair, so `Flush` applies only the latest pending operation.

A set write stores the sampled animation value and is later applied with `UiPropertyValueSource.Animation`. A clear write stores `null` for `Value` and causes the animation-source value to be cleared during flush.

During `Flush`, the store snapshots pending writes, clears the pending dictionary, applies each set or clear operation, skips unchanged effective values, and counts property writes plus render and layout invalidations from `Category`.

## Constructors

| Name | Description |
| --- | --- |
| `PendingWrite(PendingWriteKind, UiObject, UiProperty, object?, MotionPropertyInvalidationCategory)` | Initializes a staged set or clear operation with its target, property, optional value, and invalidation category. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Kind` | `PendingWriteKind` | Gets whether the staged operation sets or clears an animation-source property value. |
| `Target` | `UiObject` | Gets the object whose property will be written or cleared. |
| `Property` | `UiProperty` | Gets the UI property affected by the staged operation. |
| `Value` | `object?` | Gets the staged value for set operations; `null` for clear operations. |
| `Category` | `MotionPropertyInvalidationCategory` | Gets the render and layout invalidation category counted when the effective value changes. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Set<T>(UiObject, UiProperty<T>, T, MotionPropertyInvalidationCategory)` | `PendingWrite` | Creates a staged set operation for a typed UI property value. |
| `Clear<T>(UiObject, UiProperty<T>, MotionPropertyInvalidationCategory)` | `PendingWrite` | Creates a staged clear operation for a typed UI property's animation-source value. |

## Applies to

Cerneala motion property store internals.

## See also

- `Cerneala.UI.Motion.Properties.MotionPropertyStore`
- `Cerneala.UI.Motion.Properties.MotionPropertyKey`
- `Cerneala.UI.Motion.Properties.MotionPropertyInvalidationCategory`
- `Cerneala.UI.Core.UiObject`
- `Cerneala.UI.Core.UiProperty`
- `Cerneala.UI.Core.UiPropertyValueSource`
