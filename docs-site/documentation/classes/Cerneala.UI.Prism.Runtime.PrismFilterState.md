# PrismFilterState Class

## Definition
Namespace: `Cerneala.UI.Prism.Runtime`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Runtime/PrismStates.cs`

Exposes mutable common state for one built-in filter occurrence.

```csharp
public sealed class PrismFilterState
```

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Filter` | `PrismFilterId` | Gets the built-in filter identifier. |
| `Visible` | `bool` | Gets or sets whether the filter runs. |
| `Opacity` | `float` | Gets or sets the filtered-result mix from zero through one. |
| `BlendMode` | `PrismBlendMode` | Gets or sets the per-filter blend mode. |

## Remarks

Catalog-specific filter parameters are stored in dense typed slots addressed by generated keys. The framework and generated markup use those keys without string lookup or boxing in the update path.

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Opacity` | `ArgumentOutOfRangeException` | The assigned value is non-finite or outside zero through one. |
| `BlendMode` | `ArgumentException` | The assigned value is `PassThrough`. |

## Applies to

Filter occurrences exposed through layer, group, and backdrop state.
