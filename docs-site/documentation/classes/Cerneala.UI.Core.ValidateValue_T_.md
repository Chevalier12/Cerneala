# ValidateValue<T> Delegate

## Definition
Namespace: `Cerneala.UI.Core`

Assembly/Project: `Cerneala`

Source: `UI/Core/ValidateValue.cs`

Provides the `Cerneala.UI.Core.ValidateValue<T>` API surface.

```csharp
public delegate bool ValidateValue<in T>(T value)
```

## Type Parameters

| Name | Description |
| --- | --- |
| `T` | The value type being validated. |

## Parameters

| Name | Type | Description |
| --- | --- | --- |
| `value` | `T` | The proposed value. |

## Returns

`true` when the value is valid; otherwise, `false`.

## Remarks

This page is generated from the repository API index so the documentation surface stays aligned with the source tree.

## Applies to

Cerneala UI runtime and framework API consumers.
