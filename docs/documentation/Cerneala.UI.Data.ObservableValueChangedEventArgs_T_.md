# ObservableValueChangedEventArgs&lt;T&gt; Class

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/ObservableValue{T}.cs`

Provides the previous and current values for an `ObservableValue<T>.ValueChanged` event.

```csharp
public sealed class ObservableValueChangedEventArgs<T> : EventArgs
```

Inheritance:
`Object` -> `EventArgs` -> `ObservableValueChangedEventArgs<T>`

### Type Parameters

| Name | Description |
| --- | --- |
| `T` | The type of the observed value. |

## Examples

```csharp
using Cerneala.UI.Data;

ObservableValue<int> value = new(10);

value.ValueChanged += (_, args) =>
{
    int oldValue = args.OldValue;
    int newValue = args.NewValue;
};

value.Value = 20;
```

## Remarks

`ObservableValueChangedEventArgs<T>` is created by `ObservableValue<T>` when its stored value changes. The event data captures the value before the change in `OldValue` and the value after the change in `NewValue`.

`ObservableValue<T>` raises `ValueChanged` only when its configured equality comparer reports that the old and new values are different. Assigning an equal value does not create this event data.

The class is immutable after construction.

## Constructors

| Name | Description |
| --- | --- |
| `ObservableValueChangedEventArgs(T oldValue, T newValue)` | Initializes a new instance with the previous value and the new value. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `OldValue` | `T` | Gets the value before the change. |
| `NewValue` | `T` | Gets the value after the change. |

## Applies to

Project: `Cerneala`

## See also

- `ObservableValue<T>`
