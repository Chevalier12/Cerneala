# PrismGraphCompositionSettings Struct

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraph.cs`

Stores the immutable composition-level color and lighting values captured for one Prism graph scope.

```csharp
public readonly record struct PrismGraphCompositionSettings(
    PrismColorProfile WorkingColorProfile,
    float GlobalLightAngle,
    float GlobalLightAltitude);
```

## Examples

```csharp
using Cerneala.Drawing.Prism.Graph;

static void PrintLighting(PrismGraph graph)
{
    PrismGraphCompositionSettings settings =
        graph.Scopes[0].CompositionSettings;

    Console.WriteLine(
        $"{settings.GlobalLightAngle}/{settings.GlobalLightAltitude}");
}
```

## Remarks

`PrismGraphBuilder` snapshots these values from the scope's current `PrismCompositionState`. The snapshot belongs to the graph and does not retain mutable runtime state.

The builder validates the working color profile, requires a finite light angle, and requires a finite light altitude from zero through 90 degrees. The record constructor itself performs no validation.

## Constructors

| Name | Description |
| --- | --- |
| `PrismGraphCompositionSettings(PrismColorProfile workingColorProfile, float globalLightAngle, float globalLightAltitude)` | Creates an immutable composition-settings snapshot. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `WorkingColorProfile` | `PrismColorProfile` | Gets the working color profile used by graph color-conversion nodes. |
| `GlobalLightAngle` | `float` | Gets the shared light angle in degrees. |
| `GlobalLightAltitude` | `float` | Gets the shared light altitude in degrees. |

## Applies to

Cerneala retained Prism graph scopes and backend composition.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismGraphScope`
- `Cerneala.Drawing.Prism.Graph.PrismGraphBuilder`
- `Cerneala.UI.Prism.Runtime.PrismCompositionState`
