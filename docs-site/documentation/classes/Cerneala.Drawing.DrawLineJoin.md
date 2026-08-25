# DrawLineJoin Enum

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawStrokeStyle.cs`

Specifies how adjacent stroke segments are connected.

```csharp
public enum DrawLineJoin
```

## Examples

```csharp
DrawStrokeStyle style = new(
    join: DrawLineJoin.Miter,
    miterLimit: 4);
```

## Remarks

Miter joins automatically fall back to bevel geometry when the outer intersection exceeds the style's miter limit. Round joins are tessellated as circular fans.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Miter` | `0` | Extends outer edges to their intersection, subject to `MiterLimit`. |
| `Bevel` | `1` | Connects outer edges with a straight cutoff. |
| `Round` | `2` | Connects outer edges with a circular arc. |

## Applies To

`DrawStrokeStyle.Join`.

## See Also

- `DrawStrokeStyle`
- `DrawPen`
