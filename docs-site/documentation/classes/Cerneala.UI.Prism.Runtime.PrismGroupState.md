# PrismGroupState Class

## Definition
Namespace: `Cerneala.UI.Prism.Runtime`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Runtime/PrismStates.cs`

Exposes mutable typed values and child state for one Prism group instance.

```csharp
public sealed class PrismGroupState : PrismNodeState
```

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Children` | `IReadOnlyList<PrismNodeState>` | Gets layer and nested-group state in declaration order. |
| `Filters` | `IReadOnlyList<PrismFilterState>` | Gets group filter state. |
| `Styles` | `IReadOnlyList<PrismStyleState>` | Gets group style state. |
| `Mask` | `PrismMaskState?` | Gets the optional group mask state. |
| `Visible` | `bool` | Gets or sets whether the group and its subtree participate. |
| `Opacity` | `float` | Gets or sets complete group opacity. |
| `BlendMode` | `PrismBlendMode` | Gets or sets pass-through or isolated group blending. |

## Remarks

The group state does not own or mutate the shared `PrismGroupDefinition`. Each `PrismInstance` receives independent values.

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Opacity` | `ArgumentOutOfRangeException` | The assigned value is non-finite or outside zero through one. |

## Applies to

Per-element nested Prism group state.
