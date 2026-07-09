# PropertyAdapter<TOwner, TValue> Class

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: [`UI/Data/PropertyAdapter{TOwner,TValue}.cs`](../../UI/Data/PropertyAdapter%7BTOwner,TValue%7D.cs)

Adapts a typed owner/value pair to a small read/write API backed by delegates or by a `UiProperty<TValue>`.

```csharp
public sealed class PropertyAdapter<TOwner, TValue>
```

Inheritance:
`object` -> `PropertyAdapter<TOwner, TValue>`

## Examples
The following example adapts a regular CLR property.

```csharp
using Cerneala.UI.Data;

Counter counter = new() { Count = 3 };
PropertyAdapter<Counter, int> adapter = new(
    owner => owner.Count,
    (owner, value) => owner.Count = value);

adapter.Write(counter, 7);
int value = adapter.Read(counter);

public sealed class Counter
{
    public int Count { get; set; }
}
```

The following example adapts a retained UI property.

```csharp
using Cerneala.UI.Core;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;

PropertyAdapter<CounterElement, int> adapter =
    PropertyAdapter<CounterElement, int>.ForUiProperty<CounterElement>(CounterElement.CountProperty);

CounterElement element = new();
adapter.Write(element, 4);
int value = adapter.Read(element);

public sealed class CounterElement : UIElement
{
    public static readonly UiProperty<int> CountProperty = UiProperty<int>.Register(
        "Count",
        typeof(CounterElement),
        new UiPropertyMetadata<int>(0));
}
```

## Remarks
`PropertyAdapter<TOwner, TValue>` is a typed bridge between binding/data code and a concrete storage location. The adapter stores a getter delegate and, optionally, a setter delegate. `Read` invokes the getter for the supplied owner. `Write` invokes the setter when one was supplied.

An adapter constructed without a setter is read-only: `CanWrite` returns `false`, and `Write` throws `InvalidOperationException`.

`ForUiProperty<TUiOwner>` creates an adapter for Cerneala retained UI properties. The generated adapter reads with `UiObject.GetValue<TValue>(UiProperty<TValue>)` and writes with `UiObject.SetValue<TValue>(UiProperty<TValue>, TValue)`, so validation, coercion, read-only checks, value-change notification, and invalidation come from the normal `UiObject` property pipeline. The adapter itself does not add caching, conversion, validation, or notification.

## Type Parameters
| Name | Description |
| --- | --- |
| `TOwner` | The owner type passed to the getter and optional setter delegates. |
| `TValue` | The value type read and written by the adapter. |

## Constructors
| Name | Description |
| --- | --- |
| `PropertyAdapter(Func<TOwner, TValue> getter, Action<TOwner, TValue>? setter = null)` | Creates an adapter from a required getter and an optional setter. Throws `ArgumentNullException` when `getter` is `null`. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `CanWrite` | `bool` | Returns `true` when the adapter has a setter delegate; otherwise `false`. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `Read(TOwner owner)` | `TValue` | Reads the current value by invoking the getter delegate with `owner`. |
| `Write(TOwner owner, TValue value)` | `void` | Writes `value` by invoking the setter delegate with `owner`; throws `InvalidOperationException` when the adapter is read-only. |
| `ForUiProperty<TUiOwner>(UiProperty<TValue> property)` | `PropertyAdapter<TUiOwner, TValue>` | Creates an adapter that reads and writes `property` through `UiObject.GetValue` and `UiObject.SetValue`. `TUiOwner` must derive from `UiObject`. Throws `ArgumentNullException` when `property` is `null`. |

## Applies To
Cerneala retained UI data and binding infrastructure.

## See Also
- [`UiObject`](../../UI/Core/UiObject.cs)
- [`UiProperty<T>`](../../UI/Core/UiProperty%7BT%7D.cs)
- [`Binding<T>`](../../UI/Data/Binding%7BT%7D.cs)
