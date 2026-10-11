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
| `PrismGradientMapResource(IEnumerable<PrismGradientMapPoint> points)` | Creates a gradient with at least two points whose offsets are nondecreasing, start at `0`, and end at `1`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Points` | `ImmutableArray<PrismGradientMapPoint>` | Gets the validated color points. |

Current behavior: the SDL_GPU renderer receives no gradient-map resources, because the draw-resource set that the UI builds for it contains no gradient entries. A `GradientOverlay` style that names a gradient resource therefore draws its content unchanged; only the built-in default gradient renders. `GradientMap` resolves its reference only as an image, so a `PrismGradientMapResource` never reaches it.

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

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| Constructor | `ArgumentNullException` | `points` is `null`. |
| Constructor | `ArgumentException` | `points` has fewer than two elements, the first offset is not `0`, the last offset is not `1`, or an offset is smaller than the one before it. |
