# DrawLineCap Enum

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawStrokeStyle.cs`

Specifies the geometry placed at an open stroke endpoint.

```csharp
public enum DrawLineCap
```

## Examples

```csharp
DrawStrokeStyle style = new(
    startCap: DrawLineCap.Round,
    endCap: DrawLineCap.Triangle);
```

## Remarks

Endpoint caps apply to open contours and painted dash fragments. Closed solid contours use joins instead of endpoint caps.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Flat` | `0` | Ends at the endpoint without longitudinal extension. |
| `Square` | `1` | Extends by half the stroke thickness with a rectangular edge. |
| `Round` | `2` | Adds a semicircular endpoint. |
| `Triangle` | `3` | Extends to a triangular point. |

## Applies To

`DrawStrokeStyle.StartCap` and `DrawStrokeStyle.EndCap`.

## See Also

- `DrawStrokeStyle`
- `DrawPen`
