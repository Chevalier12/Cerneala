# PrismLight Struct

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismLightingResource.cs`

Defines one immutable, validated light consumed by the Prism
`LightingEffects` filter.

```csharp
public readonly record struct PrismLight
```

## Examples

```csharp
using System.Numerics;

PrismLight key = PrismLight.Directional(
    new Vector3(0.4f, -0.2f, 1),
    new Vector3(1, 0.9f, 0.75f),
    intensity: 2);

PrismLight fill = PrismLight.Point(
    new Vector3(0.25f, 0.75f, 0.5f),
    new Vector3(0.2f, 0.4f, 1),
    intensity: 0.5f);
```

## Remarks

Colors and intensities use linear values and may exceed `1` for HDR direct
lighting. A point position uses normalized filter coordinates: the filtered
surface occupies the XY unit square at `Z = 0`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Kind` | `PrismLightKind` | Gets the light's spatial model. |
| `LinearSrgb` | `Vector3` | Gets the finite, non-negative linear-sRGB color. |
| `Intensity` | `float` | Gets the finite, non-negative light multiplier. |
| `Direction` | `Vector3` | Gets the normalized surface-to-light direction of a directional light. |
| `Position` | `Vector3` | Gets the normalized-filter-space position of a point light. |

## Methods

| Name | Description |
| --- | --- |
| `Directional(Vector3 surfaceToLightDirection, Vector3 linearSrgb, float intensity = 1)` | Creates a directional light and normalizes its direction. |
| `Point(Vector3 position, Vector3 linearSrgb, float intensity = 1)` | Creates a point light evaluated with inverse-square attenuation. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Directional` | `ArgumentOutOfRangeException` | The direction is zero or non-finite, or the color or intensity is invalid. |
| `Point` | `ArgumentOutOfRangeException` | The position is non-finite, or the color or intensity is invalid. |
| `Direction` | `InvalidOperationException` | The light is a point light. |
| `Position` | `InvalidOperationException` | The light is directional. |

## See also

- [PrismLightKind](Cerneala.UI.Prism.Definitions.PrismLightKind.md)
- [PrismLightingResource](Cerneala.UI.Prism.Definitions.PrismLightingResource.md)
