# UiPropertyChangedEventArgs Class

## Definition
Namespace: `Cerneala.UI.Core`

Assembly/Project: `Cerneala`

Source: `UI/Core/UiPropertyChangedEventArgs.cs`

Provides data for `UiObject.PropertyChanged` notifications when the effective value of a UI property changes.

```csharp
public class UiPropertyChangedEventArgs : EventArgs
```

Inheritance:
`Object` -> `EventArgs` -> `UiPropertyChangedEventArgs`

Derived:
`UiPropertyChangedEventArgs<T>`

## Examples

The following example listens for changes to a custom UI property and reads the changed property, previous value, new value, and effective value source from the event arguments.

```csharp
using Cerneala.UI.Core;

public sealed class ExampleElement : UiObject
{
    public static readonly UiProperty<string?> TitleProperty =
        UiProperty<string?>.Register(
            "Title",
            typeof(ExampleElement),
            new UiPropertyMetadata<string?>(null));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
}

ExampleElement element = new();

element.PropertyChanged += (_, args) =>
{
    if (args.Property == ExampleElement.TitleProperty)
    {
        Console.WriteLine($"{args.OldValue} -> {args.NewValue} ({args.ValueSource})");
    }
};

element.Title = "Ready";
```

## Remarks

`UiPropertyChangedEventArgs` is the non-generic event data type used by `UiObject.PropertyChanged` and `UiObject.OnPropertyChanged`. It identifies the owner object, the UI property whose effective value changed, the old and new effective values, and the effective `UiPropertyValueSource` after the change.

Typed property changes can be delivered as `UiPropertyChangedEventArgs<T>`, which derives from this class and exposes typed `Property`, `OldValue`, and `NewValue` properties. Handlers that receive the base `UiPropertyChangedEventArgs` should treat `OldValue` and `NewValue` as `object?`.

The constructor validates `owner` and `property` and throws `ArgumentNullException` when either argument is `null`. `OldValue` and `NewValue` may be `null` when the changed property type or value permits it.

## Constructors

| Name | Description |
| --- | --- |
| `UiPropertyChangedEventArgs(UiObject owner, UiProperty property, object? oldValue, object? newValue, UiPropertyValueSource valueSource)` | Initializes a new event argument instance for a changed UI property. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Owner` | `UiObject` | Gets the object that owns the changed UI property. |
| `Property` | `UiProperty` | Gets the UI property whose effective value changed. |
| `OldValue` | `object?` | Gets the effective value before the change. |
| `NewValue` | `object?` | Gets the effective value after the change. |
| `ValueSource` | `UiPropertyValueSource` | Gets the effective value source after the change. |

## Applies to

Cerneala UI property system.

## See also

- `UiObject`
- `UiProperty`
- `UiPropertyChangedEventArgs<T>`
- `UiPropertyValueSource`
