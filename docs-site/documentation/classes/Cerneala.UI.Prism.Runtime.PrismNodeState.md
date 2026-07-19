# PrismNodeState Class

## Definition
Namespace: `Cerneala.UI.Prism.Runtime`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Runtime/PrismStates.cs`

Provides the common identity surface for per-instance layer, group, and backdrop state.

```csharp
public abstract class PrismNodeState
```

Derived types:
`PrismLayerState`, `PrismGroupState`, `PrismBackdropState`

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Id` | `PrismNodeId` | Gets the definition node identifier. |
| `Name` | `string?` | Gets the optional Motion and diagnostics name. |

## Remarks

State handles belong to one `PrismInstance` generation. A handle obtained before `ReplaceDefinition` must not be used to read or write values after replacement.

## Applies to

Per-element Prism state addressed by numeric node ID.
