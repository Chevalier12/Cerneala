# BrushMixer Class

## Definition
Namespace: `Cerneala.UI.Motion.Interpolation`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Interpolation/BrushMixer.cs`

Interpolates supported `Brush` values for the motion system.

```csharp
public sealed class BrushMixer : ValueMixer<Brush?>
```

## Examples

```csharp
BrushMixer mixer = new();
Brush? current = mixer.Mix(
    new SolidColorBrush(Color.Black),
    new SolidColorBrush(Color.White),
    0.5f);
```

## Remarks

Solid color brushes interpolate color and opacity. Linear and radial gradients interpolate only when both values have the same number of stops at matching offsets. Image, drawing, visual, incompatible, and `null` transitions keep the source value until the animation reaches its destination.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Mix(Brush? from, Brush? to, float progress)` | `Brush?` | Interpolates supported brushes and returns the source for unsupported intermediate transitions. |

## Applies to

Project: `Cerneala`
