# FilterPredicate<T> Delegate

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/FilterPredicate{T}.cs`

Provides the `Cerneala.UI.Data.FilterPredicate<T>` API surface.

```csharp
public delegate bool FilterPredicate<in T>(T item)
```

## Type Parameters

| Name | Description |
| --- | --- |
| `T` | The item type being filtered. |

## Parameters

| Name | Type | Description |
| --- | --- | --- |
| `item` | `T` | The item to test. |

## Returns

`true` when the item should be included; otherwise, `false`.

## Remarks

This page is generated from the repository API index so the documentation surface stays aligned with the source tree.

## Applies to

Cerneala UI runtime and framework API consumers.
