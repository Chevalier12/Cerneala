# PrismMaskState Class

## Definition
Namespace: `Cerneala.UI.Prism.Runtime`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Runtime/PrismStates.cs`

Exposes mutable typed mask values for one Prism scope instance.

```csharp
public sealed class PrismMaskState
```

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Image` | `PrismResourceId` | Gets or sets the backend-neutral mask-image identifier. |
| `Channel` | `PrismMaskChannel` | Gets or sets the channel used for mask coverage. |
| `Feather` | `float` | Gets or sets the nonnegative feather radius. |
| `Density` | `float` | Gets or sets mask density from zero through one. |
| `Invert` | `bool` | Gets or sets whether coverage is inverted. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Feather` | `ArgumentOutOfRangeException` | The assigned value is negative or non-finite. |
| `Density` | `ArgumentOutOfRangeException` | The assigned value is non-finite or outside zero through one. |

## Applies to

Optional layer, group, and backdrop masks.
