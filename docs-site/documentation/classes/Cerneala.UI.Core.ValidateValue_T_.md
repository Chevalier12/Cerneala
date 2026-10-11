# ValidateValue<T> Delegate

## Definition
Namespace: `Cerneala.UI.Core`

Assembly/Project: `Cerneala`

Source: `UI/Core/ValidateValue.cs`

Represents a callback that decides whether a property value is allowed.

```csharp
public delegate bool ValidateValue<in T>(T value);
```

## Examples

A property that rejects negative values:

```csharp
using Cerneala.UI.Core;

UiProperty<int> count = UiProperty<int>.Register(
    "Count",
    typeof(Holder),
    new UiPropertyMetadata<int>(0, validateValue: value => value >= 0));

Holder holder = new();
holder.SetValue(count, 3);

try
{
    holder.SetValue(count, -1);
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.Message);
}

Console.WriteLine(holder.GetValue(count));
// Value for 'Holder.Count' failed validation. (Parameter 'value')
// 3

sealed class Holder : UiObject
{
}
```

The first line shows the property's diagnostic name: the owner type's full name, a dot, and the registered name. The value stays `3`, because the rejected value is not stored.

## Remarks

Pass the callback to the `UiPropertyMetadata<T>` constructor through its `validateValue` parameter. `UiPropertyMetadata<T>.ValidateValue` returns it. Return `true` to allow the value and `false` to reject it.

The callback receives the value after `CoerceValue<T>` has run. For example, with `coerceValue: (o, v) => Math.Max(v, 0)` and `validateValue: v => v >= 0`, setting `-5` stores `0` and does not throw.

| Path | Result when the callback returns `false` |
| --- | --- |
| `UiObject.SetValue` | Throws `ArgumentException` with the message `Value for '<DiagnosticName>' failed validation.`; nothing is stored. |
| Values written through a `MotionPropertyBinding<T>` | Throw the same `ArgumentException`. |
| Animation samples that `MotionPropertyStore` writes to the `Animation` source | The sample is skipped, and the previous value stays. |
| `ElementAspect.SetValue` and the `ElementAspectValue` constructor | Throw the same `ArgumentException`. They validate the raw value; `CoerceValue<T>` does not run first. |

The callback does not run for the metadata default value or for `ClearValue`.

## Applies to

`UiPropertyMetadata<T>`.

## See also

- `Cerneala.UI.Core.CoerceValue<T>`
- `Cerneala.UI.Core.UiPropertyMetadata<T>`
- `Cerneala.UI.Core.UiObject`
