# Binding Class

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/Binding.cs`

Represents the disposable base type for observable value bindings and provides factory methods for typed one-way and two-way bindings.

```csharp
public abstract class Binding : IDisposable
```

Inheritance:
`Object` -> `Binding`

Derived:
`Binding<T>`

Implements:
`IDisposable`

## Examples
The following example creates a one-way binding from an `ObservableValue<int>` to a target variable. The target is updated immediately by default and again whenever the source value changes.

```csharp
using Cerneala.UI.Data;

ObservableValue<int> source = new(1);
int target = 0;

using Binding<int> binding = Binding.OneWay(source, value => target = value);

source.Value = 9;
// target is now 9.
```

The following example creates a two-way binding and commits a target-side value back to the source.

```csharp
using Cerneala.UI.Data;

ObservableValue<string> source = new("initial");

using Binding<string> binding = Binding.TwoWay(source, value =>
{
    // Apply the source value to the target.
});

binding.CommitTargetValue("changed");
// source.Value is now "changed".
```

## Remarks
`Binding` owns the shared lifetime contract for binding objects. Calling `Dispose` marks the binding as disposed, calls the derived `DisposeCore` implementation, and suppresses finalization. A second call to `Dispose` returns without doing extra work.

The static `OneWay` and `TwoWay` methods create `Binding<T>` instances backed by an `ObservableValue<T>`. One-way bindings listen to source changes and push each new value into the supplied target setter. Two-way bindings use the same source-to-target behavior and also configure target commits to write back through `ObservableValue<T>.SetValue`.

By default, both factory methods update the target immediately with the current source value. Pass `updateTargetImmediately: false` to subscribe without the initial target update.

`Binding` does not use string property paths. Source values are supplied directly through `ObservableValue<T>`, and target updates are supplied as typed delegates.

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `IsDisposed` | `bool` | Gets whether `Dispose` has already been called for this binding. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `Dispose()` | `void` | Disposes the binding once. The first call sets `IsDisposed`, invokes `DisposeCore`, and suppresses finalization. |
| `OneWay<T>(ObservableValue<T> source, Action<T> targetSetter, bool updateTargetImmediately = true)` | `Binding<T>` | Creates a one-way typed binding from an observable source to a target setter. |
| `TwoWay<T>(ObservableValue<T> source, Action<T> targetSetter, bool updateTargetImmediately = true)` | `Binding<T>` | Creates a two-way typed binding from an observable source to a target setter, with target commits writing back to the source. |

## Protected Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `DisposeCore()` | `void` | Releases derived binding resources. `Binding<T>` uses this to unsubscribe from `ObservableValue<T>.ValueChanged`. |
| `ThrowIfDisposed()` | `void` | Throws `ObjectDisposedException` when the binding has already been disposed. |

## Applies to
Project: `Cerneala`

Target framework: `net8.0`

## See also
- `Binding<T>`
- `BindingMode`
- `ObservableValue<T>`
