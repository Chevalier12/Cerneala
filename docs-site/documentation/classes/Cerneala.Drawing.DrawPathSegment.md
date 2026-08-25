# DrawPathSegment Struct

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawPath.cs`

Stores one immutable typed path operation.

```csharp
public readonly record struct DrawPathSegment
```

## Examples

```csharp
foreach (DrawPathSegment segment in path.Contours[0].Segments)
{
    Console.WriteLine($"{segment.Kind}: {segment.EndPoint}");
}
```

## Remarks

Payload properties not used by `Kind` contain their default values. Arc rotation uses SVG degrees; the endpoint, radii, large-arc flag, and sweep flag follow the SVG endpoint arc model.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Kind` | `DrawPathSegmentKind` | Gets the operation kind. |
| `EndPoint` | `DrawPoint` | Gets the destination point, move point, or close target. |
| `Control1` | `DrawPoint` | Gets the quadratic control or first cubic control. |
| `Control2` | `DrawPoint` | Gets the second cubic control. |
| `RadiusX` | `float` | Gets the arc x-radius. |
| `RadiusY` | `float` | Gets the arc y-radius. |
| `RotationDegrees` | `float` | Gets the SVG arc-axis rotation in degrees. |
| `IsLargeArc` | `bool` | Gets whether the larger arc sweep is selected. |
| `Sweep` | `bool` | Gets the SVG sweep flag. |

## Applies To

Immutable typed paths.

## See Also

- `DrawPathSegmentKind`
- `DrawPathContour`
