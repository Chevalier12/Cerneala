# CoerceValue<T> Delegate

## Definition
Namespace: `Cerneala.UI.Core`

Assembly/Project: `Cerneala`

Source: `UI/Core/CoerceValue.cs`

Represents a callback that adjusts a value before the property system stores it.

```csharp
public delegate T CoerceValue<T>(UiObject owner, T value);
```

## Examples

A property that clamps every value to the range 0 to 10:

```csharp
using Cerneala.UI.Core;

UiProperty<int> clamped = UiProperty<int>.Register(
    "Clamped",
    typeof(Holder),
    new UiPropertyMetadata<int>(0, coerceValue: (owner, value) => Math.Clamp(value, 0, 10)));

Holder holder = new();
holder.SetValue(clamped, 25);
Console.WriteLine(holder.GetValue(clamped));
// 10

holder.SetValue(clamped, -5);
Console.WriteLine(holder.GetValue(clamped));
// 0

sealed class Holder : UiObject
{
}
```

## Remarks

Pass the callback to the `UiPropertyMetadata<T>` constructor through its `coerceValue` parameter. `UiPropertyMetadata<T>.CoerceValue` returns it.

`owner` is the object whose value is being set. `value` is the value passed to the setter. The return value is the value that is validated and stored.

When a value is set, the property system runs these steps in order:

1. Calls `CoerceValue<T>`, if the metadata has one.
2. Calls `ValidateValue<T>` with the coerced value, if the metadata has one.
3. Stores the coerced value in the requested source.

The stored value is the coerced one, so `GetSourceValue` also returns `10` in the example above. `UiPropertyMutation.WasCoerced` is `true` when the coerced value differs from the value passed in.

The callback runs only when a value is set. It does not run for the metadata default value, for `ClearValue`, or again later when other properties change. An exception thrown by the callback propagates to the caller of `SetValue`, and nothing is stored.

## Applies to

`UiPropertyMetadata<T>`.

## See also

- `Cerneala.UI.Core.ValidateValue<T>`
- `Cerneala.UI.Core.UiPropertyMetadata<T>`
- `Cerneala.UI.Core.UiObject`
