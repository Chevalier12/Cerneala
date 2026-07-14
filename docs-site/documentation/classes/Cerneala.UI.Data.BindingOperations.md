# Cerneala.UI.Data.BindingOperations Class

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/BindingOperations.cs`

Provides factory methods for binding an `ObservableValue<T>` to a writable `UiProperty<T>` on a `UiObject`.

```csharp
public static class BindingOperations
```

Inheritance:
`object` -> `BindingOperations`

## Examples

Create a one-way binding from an observable value to a `TextBlock` property:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Data;

ObservableValue<string> source = new("Ready");
TextBlock statusText = new();

statusText.Bindings.Add(
    BindingOperations.BindOneWay(statusText, TextBlock.TextProperty, source));

source.Value = "Running";
```

Create a two-way binding for editable text:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Data;

ObservableValue<string> name = new("Ada");
TextBox nameTextBox = new();

using UiPropertyBinding<string> binding =
    BindingOperations.BindTwoWay(nameTextBox, TextBoxBase.TextProperty, name);

nameTextBox.Text = "Grace";
```

## Remarks

`BindingOperations` is a small convenience surface over `UiPropertyBinding<T>`. `BindOneWay` and `BindTwoWay` call `Bind` with `BindingMode.OneWay` and `BindingMode.TwoWay`.

For an attached `UIElement`, the binding automatically uses `UIRoot.Relay`. Overloads accepting an explicit `UiRelay` support generic `UiObject` targets and targets that are not attached yet. An explicit Relay must match the target root's Relay when that target is later attached.

All bindings write the current source value to the target during construction. After that, source changes update the target property. When the mode is `TwoWay`, changes to the target property also update the observable source.

The target, target property, and source arguments must be non-null. The target property must not be read-only. If the initial target write fails, for example because the target property's validation rejects the source value, the binding unsubscribes before the exception is rethrown.

The returned `UiPropertyBinding<T>` implements `IDisposable`. Dispose it directly, use a `using` declaration, or add it to a `UIElement.Bindings` collection so it can be disposed with the element lifecycle.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Bind<T>(UiObject target, UiProperty<T> targetProperty, ObservableValue<T> source, BindingMode mode)` | `UiPropertyBinding<T>` | Creates a `UiPropertyBinding<T>` with the specified binding mode after validating required arguments and rejecting read-only target properties. |
| `Bind<T>(UiObject target, UiProperty<T> targetProperty, ObservableValue<T> source, BindingMode mode, UiRelay relay)` | `UiPropertyBinding<T>` | Creates a binding that uses the explicit Relay for off-thread source notifications. |
| `BindOneWay<T>(UiObject target, UiProperty<T> targetProperty, ObservableValue<T> source)` | `UiPropertyBinding<T>` | Creates a one-way binding from the observable source to the target UI property. |
| `BindOneWay<T>(UiObject target, UiProperty<T> targetProperty, ObservableValue<T> source, UiRelay relay)` | `UiPropertyBinding<T>` | Creates a one-way binding with an explicit Relay. |
| `BindTwoWay<T>(UiObject target, UiProperty<T> targetProperty, ObservableValue<T> source)` | `UiPropertyBinding<T>` | Creates a two-way binding between the observable source and the target UI property. |
| `BindTwoWay<T>(UiObject target, UiProperty<T> targetProperty, ObservableValue<T> source, UiRelay relay)` | `UiPropertyBinding<T>` | Creates a two-way binding with an explicit Relay. |

## Exceptions

| Method | Exception | Condition |
| --- | --- | --- |
| `Bind<T>` | `ArgumentNullException` | `target`, `targetProperty`, or `source` is `null`. |
| `Bind<T>` with Relay | `ArgumentNullException` | `target`, `targetProperty`, `source`, or `relay` is `null`. |
| `Bind<T>` | `InvalidOperationException` | `targetProperty.IsReadOnly` is `true`. |
| Explicit-Relay overloads | `InvalidOperationException` | The target is attached later to a root owned by a different Relay. |
| `BindOneWay<T>` | `ArgumentNullException` | Propagated from `Bind<T>` when `target`, `targetProperty`, or `source` is `null`. |
| `BindOneWay<T>` | `InvalidOperationException` | Propagated from `Bind<T>` when the target property is read-only. |
| `BindTwoWay<T>` | `ArgumentNullException` | Propagated from `Bind<T>` when `target`, `targetProperty`, or `source` is `null`. |
| `BindTwoWay<T>` | `InvalidOperationException` | Propagated from `Bind<T>` when the target property is read-only. |

Exceptions thrown by the target property's setter, coercion, validation, or property-change pipeline can also propagate while the binding writes the initial source value.

## Applies to

Cerneala retained UI data binding APIs.

## See also

- `Cerneala.UI.Data.UiPropertyBinding<T>`
- `Cerneala.UI.Data.ObservableValue<T>`
- `Cerneala.UI.Data.BindingMode`
- `Cerneala.UI.Elements.UIElement.Bindings`
