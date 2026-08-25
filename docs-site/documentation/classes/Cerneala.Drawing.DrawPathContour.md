# DrawPathContour Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawPath.cs`

Represents one immutable open or closed contour in a `DrawPath`.

```csharp
public sealed class DrawPathContour
```

## Examples

```csharp
foreach (DrawPathContour contour in path.Contours)
{
    Console.WriteLine($"{contour.StartPoint}, closed: {contour.IsClosed}");
}
```

## Remarks

`Segments` is an immutable snapshot. It starts with a `Move` segment and ends with `Close` when `IsClosed` is `true`. Fill tessellation implicitly connects open contour endpoints for fill calculations without changing `IsClosed`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Segments` | `IReadOnlyList<DrawPathSegment>` | Gets the immutable typed segment sequence. |
| `StartPoint` | `DrawPoint` | Gets the contour's move point. |
| `IsClosed` | `bool` | Gets whether the contour contains an explicit close operation. |

## Applies To

Typed path inspection, fill, stroke, and clipping geometry.

## See Also

- `DrawPath`
- `DrawPathSegment`
