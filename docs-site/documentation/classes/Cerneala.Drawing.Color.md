# Color Record

## Definition
Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/Color.cs`

Represents an RGBA color used by Cerneala drawing commands.

```csharp
public readonly record struct Color(byte R, byte G, byte B, byte A = 255)
```

## Examples

```csharp
using Cerneala.Drawing;

Color opaqueRed = new(255, 0, 0);
Color transparent = Color.Transparent;
Color white = Color.White;
```

## Remarks

`Color` stores red, green, blue, and alpha channels as bytes. The alpha channel defaults to `255`, so colors constructed with three arguments are fully opaque.

Use the static presets for the complete WPF named-color catalog (including `Transparent`, `AliceBlue`, `Tomato`, and `YellowGreen`) when building draw commands, brushes, or diagnostic visuals. Names are also accepted case-insensitively by `TryParse`.

## Constructors

| Name | Description |
| --- | --- |
| `Color(byte, byte, byte, byte)` | Initializes a color from red, green, blue, and optional alpha channels. |

## Properties

| Name | Description |
| --- | --- |
| `R` | Gets the red channel. |
| `G` | Gets the green channel. |
| `B` | Gets the blue channel. |
| `A` | Gets the alpha channel. |
| `Transparent` | Gets `Color(0, 0, 0, 0)`. |
| `White` | Gets `Color(255, 255, 255, 255)`. |
| `Black` | Gets `Color(0, 0, 0, 255)`. |

The remaining WPF named colors are exposed as static properties with their standard RGB values.

## Methods

| Name | Description |
| --- | --- |
| `FromRgb(byte, byte, byte)` | Creates an opaque color. |
| `FromArgb(byte, byte, byte, byte)` | Creates a color using WPF's alpha, red, green, blue argument order. |
| `TryParse(string, out Color)` | Parses a WPF named color, `#RRGGBB`, `#AARRGGBB`, or comma-separated byte channels. |

## Applies to

Cerneala drawing primitives.

## See also

- `Cerneala.Drawing.DrawCommand`
- `Cerneala.Drawing.DrawingContext`
