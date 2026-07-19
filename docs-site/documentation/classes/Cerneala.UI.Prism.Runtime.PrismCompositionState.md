# PrismCompositionState Class

## Definition
Namespace: `Cerneala.UI.Prism.Runtime`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Runtime/PrismStates.cs`

Exposes mutable typed composition-level values for one `PrismInstance`.

```csharp
public sealed class PrismCompositionState
```

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `WorkingColorProfile` | `PrismColorProfile` | Gets or sets the working color profile. |
| `GlobalLightAngle` | `float` | Gets or sets the shared light angle in degrees. |
| `GlobalLightAltitude` | `float` | Gets or sets the shared light altitude from zero through 90 degrees. |

## Remarks

Defaults come from the canonical Prism catalog and authored definition values. A changed assignment increments `PrismInstance.ValueVersion`; an identical assignment is a no-op.

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `GlobalLightAngle` | `ArgumentOutOfRangeException` | The assigned value is non-finite. |
| `GlobalLightAltitude` | `ArgumentOutOfRangeException` | The assigned value is non-finite or outside zero through 90. |

## Applies to

Composition-wide Prism color and lighting state.
