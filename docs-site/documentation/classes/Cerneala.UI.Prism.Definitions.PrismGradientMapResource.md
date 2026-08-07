# PrismGradientMapResource Class

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismGradientMapResource.cs`

Defines a one-dimensional linear-sRGB color transfer function for Prism
gradient effects, including `GradientMap` and `GradientOverlay`.

```csharp
public sealed class PrismGradientMapResource
```

## Constructors

| Name | Description |
| --- | --- |
| `PrismGradientMapResource(IEnumerable<PrismGradientMapPoint> points)` | Creates a gradient whose offsets are nondecreasing and span zero through one. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Points` | `ImmutableArray<PrismGradientMapPoint>` | Gets the validated color points. |

The draw resource identity and version participate in retained dependencies. A missing required resource falls back to the unfiltered input.

## Examples
```csharp
PrismGradientMapResource hardStop = new(
[
    new PrismGradientMapPoint(0f, Vector3.Zero),
    new PrismGradientMapPoint(0.5f, Vector3.Zero),
    new PrismGradientMapPoint(0.5f, Vector3.One),
    new PrismGradientMapPoint(1f, Vector3.One)
]);
```

## Remarks
Equal adjacent offsets create hard stops. Colors are straight linear-sRGB;
each point carries straight alpha independently. `GradientOverlay` converts
the resulting premultiplied LUT into the composition working profile.
