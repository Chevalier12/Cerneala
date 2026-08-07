# PrismLightingResource Class

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismLightingResource.cs`

Stores the directional and point lights evaluated by the Prism
`LightingEffects` filter.

```csharp
public sealed class PrismLightingResource
```

## Examples

```csharp
using System.Numerics;

PrismLightingResource lighting = new(
[
    PrismLight.Directional(
        new Vector3(0.4f, -0.2f, 1),
        Vector3.One,
        intensity: 2),
    PrismLight.Point(
        new Vector3(0.25f, 0.75f, 0.5f),
        new Vector3(0.2f, 0.4f, 1))
]);
```

## Remarks

The resource is immutable and retains light order. `LightingEffects` evaluates
up to eight lights with a GGX normal-distribution function, correlated Smith
visibility, Schlick Fresnel, and Lambert diffuse response. Optional height-map
samples perturb the surface normal.

Assign an instance to the UI resource named by the filter's required `Lights`
resource property. Point-light XY coordinates are relative to the filtered
bounds.

## Constructors

| Name | Description |
| --- | --- |
| `PrismLightingResource(IEnumerable<PrismLight> lights)` | Creates a resource containing between one and eight lights. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `MaximumLightCount` | `int` | The CPU/GPU light limit. Its value is `8`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Lights` | `ImmutableArray<PrismLight>` | Gets the lights in deterministic evaluation order. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| Constructor | `ArgumentNullException` | `lights` is `null`. |
| Constructor | `ArgumentException` | `lights` is empty, contains more than eight entries, or contains an invalid default value. |

## See also

- [PrismLight](Cerneala.UI.Prism.Definitions.PrismLight.md)
- [PrismLightKind](Cerneala.UI.Prism.Definitions.PrismLightKind.md)
