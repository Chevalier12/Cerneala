# UiPropertyStore Class

## Definition
Namespace: `Cerneala.UI.Core`

Assembly/Project: `Cerneala`

Source: `UI/Core/UiPropertyStore.cs`

Stores UI property values by source and resolves the effective value according to Cerneala's property precedence.

```csharp
public sealed class UiPropertyStore
```

## Examples

```csharp
using Cerneala.UI.Core;

UiProperty<string> titleProperty = UiProperty<string>.Register(
    "Title",
    typeof(MyOwner),
    new UiPropertyMetadata<string>("default"));

UiPropertyStore store = new();
store.SetValue(titleProperty, UiPropertyValueSource.Inherited, "inherited");
store.SetValue(titleProperty, UiPropertyValueSource.Local, "local");

object? value = store.GetValue(titleProperty);
UiPropertyValueSource source = store.GetValueSource(titleProperty);
```

## Remarks

`UiPropertyStore` is the low-level storage used by the UI property system. It keeps one dictionary per `UiProperty`, with individual values stored by `UiPropertyValueSource`.

Effective value lookup uses a fixed precedence order: `Local`, `Animation`, `AspectVisualState`, `AspectBase`, `TemplateBinding`, and `Inherited`. If none of those concrete sources has a value, the store returns the property's default value and reports `UiPropertyValueSource.Default`.

The store accepts only concrete non-default sources for stored values. Passing `UiPropertyValueSource.Default` or an undefined enum value to `SetValue`, `GetSourceValue`, or `ClearValue` throws `ArgumentOutOfRangeException`.

## Constructors

| Name | Description |
| --- | --- |
| `UiPropertyStore()` | Initializes an empty property store. |

## Methods

| Name | Description |
| --- | --- |
| `GetValue(UiProperty)` | Gets the effective value for a property. |
| `GetValueSource(UiProperty)` | Gets the source that currently supplies the effective value. |
| `GetSourceValue(UiProperty, UiPropertyValueSource)` | Gets the value stored for a specific concrete source, or `null` when that source has no stored value. |
| `SetValue(UiProperty, UiPropertyValueSource, object?)` | Stores a value for a property at a concrete source. |
| `ClearValue(UiProperty, UiPropertyValueSource)` | Removes a value for a property at a concrete source and removes the property entry when no source values remain. |

## Applies to

Cerneala retained UI property system.

## See also

- `Cerneala.UI.Core.UiObject`
- `Cerneala.UI.Core.UiProperty`
- `Cerneala.UI.Core.UiPropertyValueSource`
