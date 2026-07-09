# UiPropertyChangedEventArgs<T> Class

## Definition
Namespace: `Cerneala.UI.Core`

Assembly/Project: `Cerneala`

Source: `UI/Core/UiPropertyChangedEventArgs{T}.cs`

Provides typed event data for a changed `UiProperty<T>` value.

```csharp
public sealed class UiPropertyChangedEventArgs<T> : UiPropertyChangedEventArgs
```

Inheritance:
`object` -> `EventArgs` -> `UiPropertyChangedEventArgs` -> `UiPropertyChangedEventArgs<T>`

## Examples

```csharp
using Cerneala.UI.Core;

owner.PropertyChanged += (_, args) =>
{
    if (args is UiPropertyChangedEventArgs<int> typedArgs)
    {
        int oldValue = typedArgs.OldValue;
        int newValue = typedArgs.NewValue;
    }
};
```

## Remarks

`UiPropertyChangedEventArgs<T>` preserves the owner, property, old value, new value, and value source from `UiPropertyChangedEventArgs`, while exposing typed `Property`, `OldValue`, and `NewValue` members.

The constructor forwards the same values to the base event args, then stores the typed property and values. Use this type when a property-change handler needs the strongly typed payload without casting the old and new values manually.

## Constructors

| Name | Description |
| --- | --- |
| `UiPropertyChangedEventArgs(UiObject, UiProperty<T>, T, T, UiPropertyValueSource)` | Initializes typed event data for a changed UI property value. |

## Properties

| Name | Description |
| --- | --- |
| `Property` | Gets the typed UI property whose effective value changed. |
| `OldValue` | Gets the previous typed value. |
| `NewValue` | Gets the new typed value. |

## Applies to

Cerneala retained UI property system.

## See also

- `Cerneala.UI.Core.UiPropertyChangedEventArgs`
- `Cerneala.UI.Core.UiProperty<T>`
- `Cerneala.UI.Core.UiObject`
