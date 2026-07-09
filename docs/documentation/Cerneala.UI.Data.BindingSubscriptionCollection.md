# BindingSubscriptionCollection Class

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/BindingSubscriptionCollection.cs`

Stores the disposable binding subscriptions owned by a UI element and disposes them when they are removed or cleared.

```csharp
public sealed class BindingSubscriptionCollection
```

Inheritance:
`Object` -> `BindingSubscriptionCollection`

## Examples

Add a binding to a `TextBlock`, then remove it to stop future source updates:

```csharp
using System;
using Cerneala.UI.Controls;
using Cerneala.UI.Data;

ObservableValue<string> source = new("Ready");
TextBlock statusText = new();

IDisposable binding = BindingOperations.BindOneWay(
    statusText,
    TextBlock.TextProperty,
    source);

statusText.Bindings.Add(binding);
source.Value = "Running";

statusText.Bindings.Remove(binding);
source.Value = "Stopped";
```

## Remarks

`BindingSubscriptionCollection` is the lifetime container exposed by `UIElement.Bindings`. It is intended for bindings created with APIs such as `BindingOperations.BindOneWay` and `BindingOperations.BindTwoWay`.

Adding a subscription stores the `IDisposable` instance unless the same instance is already present. Duplicate adds are ignored.

Removing a stored subscription removes it from the collection and disposes it. Removing a subscription that is not in the collection returns `false` and does not dispose it.

Clearing the collection disposes every subscription that was present at the time of the clear operation. `UIElement.DetachFromRoot` calls `Bindings.Clear()`, so element-owned bindings stop receiving source changes after the element is detached from its root.

The collection is a simple owner, not an enumerable list. Use `Count` to inspect how many subscriptions are currently stored.

## Constructors

| Name | Description |
| --- | --- |
| `BindingSubscriptionCollection()` | Initializes an empty binding subscription collection. |

## Properties

| Name | Description |
| --- | --- |
| `Count` | Gets the number of stored binding subscriptions. |

## Methods

| Name | Description |
| --- | --- |
| `Add(IDisposable binding)` | Adds a binding subscription when the same instance is not already stored. |
| `Remove(IDisposable binding)` | Removes and disposes a stored binding subscription, returning `true` when it was found. |
| `Clear()` | Removes and disposes all stored binding subscriptions. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Add(IDisposable binding)` | `ArgumentNullException` | `binding` is `null`. |
| `Remove(IDisposable binding)` | `ArgumentNullException` | `binding` is `null`. |

## Applies to

Cerneala retained UI data binding APIs.

## See also

- `Cerneala.UI.Elements.UIElement.Bindings`
- `Cerneala.UI.Data.BindingOperations`
- `Cerneala.UI.Data.UiPropertyBinding<T>`
- `Cerneala.UI.Data.ObservableValue<T>`
