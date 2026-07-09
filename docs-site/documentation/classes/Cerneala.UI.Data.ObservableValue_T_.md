# ObservableValue<T> Class

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/ObservableValue{T}.cs`

Represents a single mutable value that raises an event when the effective value changes.

```csharp
public sealed class ObservableValue<T>
```

## Examples

```csharp
using Cerneala.UI.Data;

ObservableValue<int> count = new(0);
count.ValueChanged += (_, args) =>
{
    int oldValue = args.OldValue;
    int newValue = args.NewValue;
};

int previous = count.SetValue(3);
count.Value = 4;
```

## Remarks

`ObservableValue<T>` stores a value and compares assignments with an `IEqualityComparer<T>`. If no comparer is supplied, it uses `EqualityComparer<T>.Default`.

`SetValue` returns the previous value. When the comparer says the previous value and the new value are equal, the stored value is left unchanged and `ValueChanged` is not raised. Otherwise, the stored value is updated and `ValueChanged` is raised with typed old and new values.

## Constructors

| Name | Description |
| --- | --- |
| `ObservableValue(T, IEqualityComparer<T>?)` | Initializes the observable value with an optional equality comparer. |

## Properties

| Name | Description |
| --- | --- |
| `Value` | Gets or sets the current value. Setting delegates to `SetValue`. |

## Methods

| Name | Description |
| --- | --- |
| `SetValue(T)` | Sets the value when it differs from the current value and returns the previous value. |

## Events

| Name | Description |
| --- | --- |
| `ValueChanged` | Raised after the stored value changes. |

## Applies to

Cerneala retained UI data binding helpers.

## See also

- `Cerneala.UI.Data.ObservableValueChangedEventArgs<T>`
- `Cerneala.UI.Data.Binding<T>`
