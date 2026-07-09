# ContentValueEqualityComparer Class

## Definition
Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/ContentControl.cs`

Compares content values for `ContentControl` while treating UI elements by reference ownership semantics.

```csharp
private sealed class ContentValueEqualityComparer : IEqualityComparer<object?>
```

Implements:
`IEqualityComparer<object?>`

## Remarks

`ContentValueEqualityComparer` is a private nested implementation detail of `ContentControl`. It returns `true` for reference-equal values, returns `false` when either side is a `UIElement` but the references differ, and otherwise falls back to `EqualityComparer<object?>.Default`.

This behavior keeps retained UI element content from being treated as value-equal when the instance changes.

## Methods

| Name | Description |
| --- | --- |
| `Equals(object?, object?)` | Compares two content values using reference semantics for `UIElement` instances and default equality for other values. |
| `GetHashCode(object?)` | Returns the value hash code or `0` for `null`. |

## Applies to

Project: `Cerneala`

## See also

- Source: `UI/Controls/ContentControl.cs`
- `ContentControl`
