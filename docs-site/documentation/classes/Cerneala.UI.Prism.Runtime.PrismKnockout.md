# PrismKnockout Enum

## Definition
Namespace: `Cerneala.UI.Prism.Runtime`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Runtime/PrismAdvancedBlend.cs`

Specifies Photoshop-style knockout behavior for a Prism layer.

```csharp
public enum PrismKnockout
```

## Values

| Name | Description |
| --- | --- |
| `None` | Performs no knockout. |
| `Shallow` | Knocks out to the current isolated group boundary. |
| `Deep` | Knocks out through deeper accumulated content. |

## Applies to

`PrismLayerState.Knockout`.
