# DrawStrokeStyle Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawStrokeStyle.cs`

Defines immutable cap, join, miter, dash, and alignment settings for a `DrawPen`.

```csharp
public sealed class DrawStrokeStyle
```

## Examples

```csharp
DrawStrokeStyle dashedOutline = new(
    startCap: DrawLineCap.Round,
    endCap: DrawLineCap.Round,
    join: DrawLineJoin.Miter,
    miterLimit: 4,
    dashPattern: [6, 3, 1, 3],
    dashOffset: 2,
    alignment: DrawStrokeAlignment.Outside);
```

## Remarks

Dash entries are positive lengths in logical drawing units and alternate painted and skipped portions. An odd-length pattern is repeated to produce an even cycle. `DashOffset` selects the initial phase, wraps through the complete cycle, and continues across line, curve, and contour segments.

`Inside` and `Outside` apply to closed contours according to their winding direction. Open contours always use centered alignment because they have no interior. Caps close the ends of open paths and individual painted dash fragments. A miter that exceeds `MiterLimit` falls back to a bevel join.

The constructor copies `dashPattern`; modifying the source sequence later does not change the style. All enum values must be defined, `MiterLimit` must be finite and at least `1`, every dash value must be positive and finite, and `DashOffset` must be finite.

## Constructors

| Name | Description |
| --- | --- |
| `DrawStrokeStyle(DrawLineCap startCap = Flat, DrawLineCap endCap = Flat, DrawLineJoin join = Miter, float miterLimit = 10, IEnumerable<float>? dashPattern = null, float dashOffset = 0, DrawStrokeAlignment alignment = Center)` | Creates a validated immutable stroke style. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Default` | `DrawStrokeStyle` | Gets the shared flat-cap, miter-join, solid, centered default style. |
| `StartCap` | `DrawLineCap` | Gets the cap used at the beginning of an open stroke. |
| `EndCap` | `DrawLineCap` | Gets the cap used at the end of an open stroke. |
| `Join` | `DrawLineJoin` | Gets the geometry used at adjacent stroke segments. |
| `MiterLimit` | `float` | Gets the maximum accepted miter ratio before bevel fallback. |
| `DashPattern` | `IReadOnlyList<float>` | Gets the copied alternating painted and skipped lengths. |
| `DashOffset` | `float` | Gets the phase offset into the dash cycle. |
| `Alignment` | `DrawStrokeAlignment` | Gets the closed-contour alignment; open contours are centered. |

## Applies To

All native vector strokes created with `DrawPen`.

## See Also

- `DrawPen`
- `DrawLineCap`
- `DrawLineJoin`
- `DrawStrokeAlignment`
