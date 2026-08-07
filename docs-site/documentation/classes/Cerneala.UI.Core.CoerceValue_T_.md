# CoerceValue<T> Delegate

## Definition
Namespace: `Cerneala.UI.Core`

Assembly/Project: `Cerneala`

Source: `UI/Core/CoerceValue.cs`

Provides the `Cerneala.UI.Core.CoerceValue<T>` API surface.

```csharp
public delegate T CoerceValue<T>(UiObject owner, T value)
```

## Type Parameters

| Name | Description |
| --- | --- |
| `T` | The value type being coerced. |

## Parameters

| Name | Type | Description |
| --- | --- | --- |
| `owner` | `UiObject` | The object whose property value is being coerced. |
| `value` | `T` | The proposed value. |

## Returns

The effective value to store.

## Remarks

This page is generated from the repository API index so the documentation surface stays aligned with the source tree.

## Applies to

Cerneala UI runtime and framework API consumers.
