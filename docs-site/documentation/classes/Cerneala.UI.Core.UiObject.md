# UiObject Class

## Definition
Namespace: `Cerneala.UI.Core`

Assembly/Project: `Cerneala`

Source: `UI/Core/UiObject.cs`

Provides the base storage, precedence, validation, coercion, and change notification behavior for Cerneala UI properties.

```csharp
public class UiObject
```

Inheritance:
`object` -> `UiObject`

Derived:
`UIElement`

## Examples

Define a custom object with a registered UI property and observe effective value changes:

```csharp
using Cerneala.UI.Core;

public sealed class MeterObject : UiObject
{
    public static readonly UiProperty<int> CountProperty =
        UiProperty<int>.Register(
            nameof(Count),
            typeof(MeterObject),
            new UiPropertyMetadata<int>(
                defaultValue: 0,
                validateValue: value => value >= 0));

    public int Count
    {
        get => GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }
}

MeterObject meter = new();
meter.PropertyChanged += (_, args) =>
{
    if (args.Property == MeterObject.CountProperty)
    {
        Console.WriteLine($"{args.OldValue} -> {args.NewValue}");
    }
};

int oldValue = meter.SetValue(MeterObject.CountProperty, 4);
UiPropertyValueSource source = meter.GetValueSource(MeterObject.CountProperty);
```

Set and clear a lower-priority value source without replacing the local value:

```csharp
using Cerneala.UI.Core;

MeterObject meter = new();

meter.SetValue(MeterObject.CountProperty, 2, UiPropertyValueSource.Inherited);
meter.SetValue(MeterObject.CountProperty, 7, UiPropertyValueSource.Local);

int effectiveValue = meter.GetValue(MeterObject.CountProperty); // 7
object? inheritedValue = meter.GetSourceValue(
    MeterObject.CountProperty,
    UiPropertyValueSource.Inherited); // 2

meter.ClearValue(MeterObject.CountProperty, UiPropertyValueSource.Local);
effectiveValue = meter.GetValue(MeterObject.CountProperty); // 2
```

## Remarks

Derived objects can enforce mutation affinity before their property store is read or changed. Attached `UIElement` instances use this hook to reject typed and untyped `SetValue` and `ClearValue` calls from any thread other than the owning root's Relay thread; plain `UiObject` instances retain their existing unrestricted behavior.

`UiObject` is the base for objects that participate in Cerneala's UI property system. It stores values in a private `UiPropertyStore` and resolves an effective value from multiple `UiPropertyValueSource` layers.

Effective values are resolved in this order, from highest to lowest priority: `Local`, `Animation`, `AspectVisualState`, `AspectBase`, `TemplateBinding`, and `Inherited`. When no stored value exists for a property, `GetValue` returns the property's metadata default value and `GetValueSource` returns `Default`.

`SetValue` coerces the supplied value through the property's metadata, validates the coerced value, stores it for the requested source, and returns the previous effective value. `ClearValue` removes the stored value for the requested source and also returns the previous effective value. `Default` is not a storable source; storing or clearing that source is rejected by the underlying store.

Public `SetValue` and `ClearValue` overloads that take `UiProperty<T>` reject read-only properties with `InvalidOperationException`. Read-only properties can be changed only through a matching `UiPropertyKey<T>` overload.

`PropertyChanged` is raised only when a set or clear operation changes the effective value according to the property's equality comparer. Derived classes can override `OnPropertyChanged` to add behavior around that event. After an effective change, if the property has invalidation options such as `AffectsMeasure`, `AffectsArrange`, `AffectsRender`, `AffectsHitTest`, `AffectsAspect`, `AffectsInputVisual`, `AffectsSemantics`, or `Inherits`, and the object implements `IUiPropertyOwner`, `OnPropertyInvalidated` is called with those options.

`MutationObserver` is `null` by default. When a derived object supplies an observer, `UiObject` reports each set or clear mutation, including mutations that do not change the effective value.

## Constructors

| Name | Description |
| --- | --- |
| `UiObject()` | Initializes a new `UiObject` with an empty UI property store. |

## Events

| Name | Type | Description |
| --- | --- | --- |
| `PropertyChanged` | `EventHandler<UiPropertyChangedEventArgs>?` | Raised after a UI property operation changes the effective value. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetValue<T>(UiProperty<T> property)` | `T` | Gets the effective typed value for `property`, or its default value when no source has a stored value. |
| `GetValue(UiProperty property)` | `object?` | Gets the effective value for `property` without a generic type parameter. |
| `GetValueSource(UiProperty property)` | `UiPropertyValueSource` | Gets the source that currently provides the effective value. |
| `GetSourceValue(UiProperty property, UiPropertyValueSource source)` | `object?` | Gets the stored value for one concrete source, or `null` when that source has no stored value. |
| `SetValue<T>(UiProperty<T> property, T value)` | `T` | Sets a local value and returns the previous effective value. |
| `SetValue<T>(UiProperty<T> property, T value, UiPropertyValueSource source)` | `T` | Sets a value for the specified concrete source and returns the previous effective value. |
| `SetValue<T>(UiPropertyKey<T> key, T value)` | `T` | Sets a local value for a read-only property through its key and returns the previous effective value. |
| `SetValue<T>(UiPropertyKey<T> key, T value, UiPropertyValueSource source)` | `T` | Sets a value for a read-only property through its key and the specified concrete source, then returns the previous effective value. |
| `ClearValue<T>(UiProperty<T> property)` | `T` | Clears the local value and returns the previous effective value. |
| `ClearValue<T>(UiProperty<T> property, UiPropertyValueSource source)` | `T` | Clears the value stored for the specified concrete source and returns the previous effective value. |

## Protected Properties

| Name | Type | Description |
| --- | --- | --- |
| `MutationObserver` | `UiPropertyMutationObserver?` | Gets the observer that receives set and clear mutation records. The default value is `null`. |

## Protected Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `OnPropertyChanged(UiPropertyChangedEventArgs args)` | `void` | Raises `PropertyChanged`. Derived classes can override this method to react to effective property changes. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `GetValue`, `GetValueSource`, `GetSourceValue`, `SetValue`, `ClearValue` | `ArgumentNullException` | The supplied property or key is `null`. |
| `SetValue`, `ClearValue` | `InvalidOperationException` | The overload takes `UiProperty<T>` and the property is read-only. |
| `SetValue` | `ArgumentException` | The value is not assignable to the property's value type, or the coerced value fails validation. |
| `SetValue`, `ClearValue`, `GetSourceValue` | `ArgumentOutOfRangeException` | The supplied source is not a defined concrete source, or is `UiPropertyValueSource.Default`. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `UI/Core/UiObject.cs`
- `UI/Core/UiProperty.cs`
- `UI/Core/UiProperty{T}.cs`
- `UI/Core/UiPropertyMetadata{T}.cs`
- `UI/Core/UiPropertyStore.cs`
- `UI/Core/UiPropertyValueSource.cs`
- `UI/Elements/UIElement.cs`
