# PrismBackdropState Class

## Definition
Namespace: `Cerneala.UI.Prism.Runtime`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Runtime/PrismStates.cs`

Exposes mutable typed values for the optional backdrop plane in one `PrismInstance`.

```csharp
public sealed class PrismBackdropState : PrismNodeState
```

## Remarks

This state belongs to one `PrismInstance`; it owns typed values, not textures or
other GPU resources. Setting `Visible` to `false` suppresses backdrop acquisition
and processing for the scope.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Filters` | `IReadOnlyList<PrismFilterState>` | Gets backdrop filter state in declared order. |
| `Styles` | `IReadOnlyList<PrismStyleState>` | Gets backdrop style state in declared order. |
| `Mask` | `PrismMaskState?` | Gets the optional backdrop mask state. |
| `Visible` | `bool` | Gets or sets whether backdrop acquisition and processing participate. |
| `Opacity` | `float` | Gets or sets complete backdrop opacity. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Opacity` | `ArgumentOutOfRangeException` | The assigned value is non-finite or outside zero through one. |

## Applies to

Per-element backdrop processing state.
