# UiPropertyBinding<T> Class

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/UiPropertyBinding{T}.cs`

Binds an `ObservableValue<T>` source to a writable `UiProperty<T>` on a `UiObject`.

```csharp
public sealed class UiPropertyBinding<T> : Binding
```

Inheritance:
`object` -> `Binding` -> `UiPropertyBinding<T>`

Implements:
`IDisposable` through `Binding`

## Examples

Create a one-way binding from an observable value to a `TextBlock` property:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Data;

ObservableValue<string> status = new("Ready");
TextBlock statusText = new();

using UiPropertyBinding<string> binding =
    BindingOperations.BindOneWay(statusText, TextBlock.TextProperty, status);

status.Value = "Running";
```

Create a two-way binding that writes target-side changes back to the source:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Data;

ObservableValue<string> name = new("Ada");
TextBox editor = new();

using UiPropertyBinding<string> binding =
BindingOperations.BindTwoWay(editor, TextBox.TextProperty, name);

editor.Text = "Grace";
```

## Remarks

`UiPropertyBinding<T>` subscribes to `ObservableValue<T>.ValueChanged` and writes source changes to the target UI property while the binding is active. Construction also writes the current source value to the target immediately.

Source notifications already raised on the UI thread remain synchronous. For an attached `UIElement`, worker-thread notifications are coalesced through `UIRoot.Relay`; the source's current value is read only when the Relay callback runs. Detach invalidates queued callbacks, and reattach performs a complete refresh with a new activation generation.

When `Mode` is `BindingMode.TwoWay`, the binding also listens to `UiObject.PropertyChanged` on the target. If the changed property is the bound `UiProperty<T>`, the new target value is cast to `T` and written back to the source with `ObservableValue<T>.SetValue`.

The binding guards source-to-target and target-to-source updates with an internal update flag, so a write triggered by one side is not immediately echoed back by the other side. A worker-thread echo is scheduled before that guard is considered, so a later source value is not lost. Disposing the binding removes subscriptions and makes queued callbacks no-ops.

The target property must be writable. If the constructor fails while applying the initial source value, the binding removes any subscriptions it already added before rethrowing the exception.

## Constructors

| Name | Description |
| --- | --- |
| `UiPropertyBinding(UiObject target, UiProperty<T> targetProperty, ObservableValue<T> source, BindingMode mode = BindingMode.OneWay)` | Initializes the binding, subscribes to the source, optionally subscribes to target property changes for two-way mode, and writes the current source value to the target. |

## Properties

| Name | Description |
| --- | --- |
| `Mode` | Gets the binding mode used by this binding. |
| `IsDisposed` | Gets whether the binding has been disposed. Inherited from `Binding`. |

## Methods

| Name | Description |
| --- | --- |
| `Dispose()` | Disposes the binding and unsubscribes from source and target events. Inherited from `Binding`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `UiPropertyBinding(...)` | `ArgumentNullException` | `target`, `targetProperty`, or `source` is `null`. |
| `UiPropertyBinding(...)` | `InvalidOperationException` | `targetProperty.IsReadOnly` is `true`. |
| Source notification | `InvalidOperationException` | The notification is raised off-thread and neither an attached target Relay nor an explicit Relay is available. |
| `UiPropertyBinding(...)` | Any exception from the target property pipeline | The initial write to the target fails, such as when validation rejects the source value. |

## Applies to

Cerneala retained UI data binding APIs.

## See also

- `Cerneala.UI.Data.Binding`
- `Cerneala.UI.Data.BindingOperations`
- `Cerneala.UI.Data.BindingMode`
- `Cerneala.UI.Data.ObservableValue<T>`
- `Cerneala.UI.Core.UiObject`
- `Cerneala.UI.Core.UiProperty<T>`
