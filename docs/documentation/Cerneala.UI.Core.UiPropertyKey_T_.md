# UiPropertyKey<T> Class

## Definition
Namespace: `Cerneala.UI.Core`

Assembly/Project: `Cerneala`

Source: `UI/Core/UiPropertyKey{T}.cs`

Represents the authorization key used to set a read-only typed UI property.

```csharp
public sealed class UiPropertyKey<T>
```

Inheritance:
`object` -> `UiPropertyKey<T>`

## Type Parameters

| Name | Description |
| --- | --- |
| `T` | The CLR value type of the read-only UI property exposed by the key. |

## Examples

```csharp
using Cerneala.UI.Core;

public sealed class CounterObject : UiObject
{
    private static readonly UiPropertyKey<int> CountPropertyKey =
        UiProperty<int>.RegisterReadOnly(
            nameof(Count),
            typeof(CounterObject),
            new UiPropertyMetadata<int>(0));

    public static readonly UiProperty<int> CountProperty = CountPropertyKey.Property;

    public int Count => GetValue(CountProperty);

    public void UpdateCount(int value)
    {
        SetValue(CountPropertyKey, value);
    }
}
```

## Remarks

`UiPropertyKey<T>` is returned by `UiProperty<T>.RegisterReadOnly` and `UiPropertyRegistry.RegisterReadOnly`. The registry creates the underlying `UiProperty<T>` with the `UiPropertyOptions.ReadOnly` flag, then wraps it in a key.

Public `UiObject.SetValue` overloads that take `UiProperty<T>` reject read-only properties. Code that owns the matching `UiPropertyKey<T>` can call the key-based `UiObject.SetValue` overloads to update the read-only property while exposing only the `Property` descriptor to consumers.

The constructor is internal, so application code cannot create arbitrary keys. Keep the key private or otherwise limited to the owner that is allowed to mutate the read-only property.

## Constructors

| Name | Description |
| --- | --- |
| None | `UiPropertyKey<T>` has no public constructors. Use `UiProperty<T>.RegisterReadOnly` or `UiPropertyRegistry.RegisterReadOnly` to create a key. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Property` | `UiProperty<T>` | Gets the read-only UI property associated with this key. |

## Applies to

Cerneala retained UI property system.

## See also

- `Cerneala.UI.Core.UiObject`
- `Cerneala.UI.Core.UiProperty<T>`
- `Cerneala.UI.Core.UiPropertyRegistry`
- `Cerneala.UI.Core.UiPropertyOptions`
