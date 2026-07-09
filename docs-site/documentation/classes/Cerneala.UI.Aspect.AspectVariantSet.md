# AspectVariantSet Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectVariantSet.cs`

Represents an immutable set of aspect variant values keyed by `AspectVariantKey`.

```csharp
public sealed class AspectVariantSet : IEquatable<AspectVariantSet>
```

Inheritance:
`object` -> `AspectVariantSet`

Implements:
`IEquatable<AspectVariantSet>`

## Examples

Create a variant key, store a value, and read it back with the typed API:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectVariantKey<Button, ButtonKind> key = AspectVariantKey.For<Button, ButtonKind>("kind");

AspectVariantSet variants = AspectVariantSet.Empty.Set(key, ButtonKind.Primary);

if (variants.TryGet(key, out ButtonKind kind))
{
    // kind is ButtonKind.Primary.
}
```

Use a control's variant set through `SetAspectVariant` so aspect processing is invalidated when the value changes:

```csharp
using Cerneala.UI.Controls;

Button button = new();
button.SetAspectVariant(ButtonVariants.Kind, ButtonKind.Primary);
button.SetAspectVariant(ButtonVariants.Size, ButtonSize.Large);
```

## Remarks

`AspectVariantSet` stores per-element variant values used by aspect matching. `Control` keeps the current set in `AspectVariants`, and `AspectProcessor` passes that set into `AspectEngine`. Variant conditions created with `AspectCondition.Variant` match only when the key is present and the stored value equals the expected value.

The set is immutable from the public API. `Set` copies the current key/value dictionary and returns a new `AspectVariantSet`; `Empty` is the shared empty instance. `Control.SetAspectVariant` compares the old and new sets and invalidates aspect and render work only when the set changes.

Typed keys carry an owner type and value type. The typed `Set<TControl, TValue>` overload accepts the typed key and value directly. The untyped `Set(AspectVariantKey, object?)` overload validates non-null values against `key.ValueType` and throws `ArgumentException` when the value type does not match. Both `Set` and `TryGet` throw `ArgumentNullException` for a null key.

`TryGet` returns `true` only when the key exists and the stored value can be returned as `TValue`. A stored `null` value can be read successfully when `TValue` can be null. Equality compares the same keys and values, regardless of dictionary enumeration order; `GetHashCode` orders entries by key text for stable order-independent hashing.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Empty` | `AspectVariantSet` | Gets the shared empty variant set. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Equals(AspectVariantSet? other)` | `bool` | Returns whether this set and `other` contain the same variant keys with equal values. |
| `Equals(object? obj)` | `bool` | Returns whether `obj` is an `AspectVariantSet` with the same variant values. |
| `GetHashCode()` | `int` | Returns a hash code based on the contained keys and values. |
| `Set<TControl, TValue>(AspectVariantKey<TControl, TValue> key, TValue value)` | `AspectVariantSet` | Returns a new set with `value` assigned to the typed variant `key`. |
| `Set(AspectVariantKey key, object? value)` | `AspectVariantSet` | Returns a new set with `value` assigned to `key`, validating non-null values against `key.ValueType`. |
| `TryGet<TControl, TValue>(AspectVariantKey<TControl, TValue> key, out TValue value)` | `bool` | Attempts to read the value stored for the typed variant `key`. |

## Applies to

Cerneala UI aspect resolution, variant-based aspect conditions, and control variant styling.

## See also

- `Cerneala.UI.Aspect.AspectVariantKey`
- `Cerneala.UI.Aspect.AspectCondition`
- `Cerneala.UI.Aspect.AspectMatchContext`
- `Cerneala.UI.Controls.Control`
