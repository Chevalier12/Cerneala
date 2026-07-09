# Unset Class

## Definition
Namespace: `Cerneala.UI.Core`

Assembly/Project: `Cerneala`

Source: `UI/Core/Unset.cs`

Provides an internal sentinel object used to represent an unset value.

```csharp
internal static class Unset
```

## Examples

```csharp
object marker = Unset.Value;
string text = marker.ToString(); // "<unset>"
```

## Remarks

`Unset` exposes a single shared sentinel through `Value`. The object is an instance of the private nested `UnsetValue` class, so callers compare or pass the sentinel as an opaque object instead of constructing their own marker.

The sentinel's `ToString` implementation returns `"<unset>"`, which makes diagnostic output clearer when an internal value has not been assigned.

## Properties

| Name | Description |
| --- | --- |
| `Value` | Gets the shared sentinel object that represents an unset value. |

## Applies to

Cerneala retained UI internals.

## See also

- `Cerneala.UI.Core.UiPropertyStore`
- `Cerneala.UI.Core.UiObject`
