# Binding<T> Class

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/Binding{T}.cs`

Creates a disposable typed binding between an `ObservableValue<T>` source and a target setter.

```csharp
public sealed class Binding<T> : Binding
```

Inheritance:
`object` -> `Binding` -> `Binding<T>`

Implements:
`IDisposable` through `Binding`

## Examples

Create a one-way binding that copies source changes into a target value.

```csharp
using Cerneala.UI.Data;

ObservableValue<int> source = new(1);
int target = 0;

using Binding<int> binding = Binding.OneWay(source, value => target = value);

source.Value = 9;
// target is now 9.
```

Commit a target-side value back to the source by using a two-way binding.

```csharp
using Cerneala.UI.Data;

ObservableValue<int> source = new(1);

using Binding<int> binding = Binding.TwoWay(source, _ => { });
binding.CommitTargetValue(5);

// source.Value is now 5.
```

Create a converted one-way binding with an `IValueConverter<TIn, TOut>`.

```csharp
using Cerneala.UI.Data;

ObservableValue<int> source = new(2);
string target = string.Empty;

using Binding<int> binding = Binding<int>.OneWayConverted(
    source,
    value => target = value,
    new IntToStringConverter());

source.Value = 12;
// target is now "12".

sealed class IntToStringConverter : IValueConverter<int, string>
{
    public string Convert(int value) => value.ToString();

    public int ConvertBack(string value) => int.Parse(value);
}
```

## Remarks

`Binding<T>` subscribes to `ObservableValue<T>.ValueChanged` and calls the target setter with the new source value while the binding is not disposed. By default, the constructor also applies the current source value to the target immediately.

Disposing the binding unsubscribes it from the source, so later source changes no longer update the target. If the immediate target update throws during construction, the binding removes its source subscription before rethrowing.

`CommitTargetValue` is only valid for `BindingMode.TwoWay`. When a two-way binding is created without an explicit source writer, the constructor writes committed values by calling `ObservableValue<T>.SetValue`.

`OneWayConverted<TIn, TOut>` converts source values with `IValueConverter<TIn, TOut>.Convert` before passing them to the target setter. It does not use `ConvertBack`.

## Constructors

| Name | Description |
| --- | --- |
| `Binding(ObservableValue<T> source, Action<T> targetSetter, BindingMode mode = BindingMode.OneWay, Action<T>? sourceWriter = null, bool updateTargetImmediately = true)` | Initializes a typed binding, subscribes to the source, and optionally pushes the current source value to the target immediately. Throws `ArgumentNullException` when `source` or `targetSetter` is `null`. |

## Properties

| Name | Description |
| --- | --- |
| `Mode` | Gets the binding mode used by this instance. |
| `IsDisposed` | Gets whether the binding has been disposed. Inherited from `Binding`. |

## Methods

| Name | Description |
| --- | --- |
| `CommitTargetValue(T value)` | Writes a target-side value back to the source writer for two-way bindings. Throws `ObjectDisposedException` after disposal and `InvalidOperationException` when the binding is not two-way or has no source writer. |
| `OneWayConverted<TIn, TOut>(ObservableValue<TIn> source, Action<TOut> targetSetter, IValueConverter<TIn, TOut> converter, bool updateTargetImmediately = true)` | Creates a one-way binding that converts source values before assigning them to the target. Throws `ArgumentNullException` when `targetSetter` or `converter` is `null`. |
| `Dispose()` | Disposes the binding and unsubscribes it from source updates. Inherited from `Binding`. |

## Applies to

`Cerneala` UI data binding infrastructure.

## See also

- `Binding`
- `BindingMode`
- `ObservableValue<T>`
- `IValueConverter<TIn, TOut>`
