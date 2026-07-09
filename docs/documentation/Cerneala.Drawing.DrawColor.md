# DrawColor Record

## Definition
Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `UI/Drawing/DrawColor.cs`

Represents an RGBA color used by Cerneala drawing commands.

```csharp
public readonly record struct DrawColor(byte R, byte G, byte B, byte A = 255)
```

## Examples

```csharp
using Cerneala.Drawing;

DrawColor opaqueRed = new(255, 0, 0);
DrawColor transparent = DrawColor.Transparent;
DrawColor white = DrawColor.White;
```

## Remarks

`DrawColor` stores red, green, blue, and alpha channels as bytes. The alpha channel defaults to `255`, so colors constructed with three arguments are fully opaque.

Use the static presets for common transparent, white, and black values when building draw commands, brushes, or diagnostic visuals.

## Constructors

| Name | Description |
| --- | --- |
| `DrawColor(byte, byte, byte, byte)` | Initializes a color from red, green, blue, and optional alpha channels. |

## Properties

| Name | Description |
| --- | --- |
| `R` | Gets the red channel. |
| `G` | Gets the green channel. |
| `B` | Gets the blue channel. |
| `A` | Gets the alpha channel. |
| `Transparent` | Gets `DrawColor(0, 0, 0, 0)`. |
| `White` | Gets `DrawColor(255, 255, 255, 255)`. |
| `Black` | Gets `DrawColor(0, 0, 0, 255)`. |

## Applies to

Cerneala drawing primitives.

## See also

- `Cerneala.Drawing.DrawCommand`
- `Cerneala.Drawing.DrawingContext`
