# TextShapeResult Struct

## Definition
Namespace: `Cerneala.Drawing.Text`

Assembly/Project: `Cerneala` (`Cerneala.csproj`)

Source: `UI/Drawing/Text/TextShapeResult.cs`

Stores the shaped glyph data and text metrics produced by the drawing text pipeline.

```csharp
public readonly record struct TextShapeResult
```

Inheritance:
`Object` -> `ValueType` -> `TextShapeResult`

## Examples

Create a shaped result from explicit glyph ids and glyph positions:

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Text;

ushort[] glyphIds = [42, 43];
DrawPoint[] glyphPositions =
[
    new DrawPoint(0, 0),
    new DrawPoint(8, 0),
];

TextShapeResult result = new(
    "Hi",
    glyphCount: 2,
    glyphIds,
    glyphPositions,
    advanceWidth: 16,
    originOffset: new DrawPoint(0, -12));

ushort[] idsForRendering = result.GlyphIds;
DrawPoint[] positionsForRendering = result.GlyphPositions;
```

## Remarks

`TextShapeResult` is the value object passed between text shaping, rasterization, and text layout code. `SkiaTextShaper.Shape` fills it from HarfBuzz glyph ids and positions, then `SkiaTextRasterizer` uses those values to build a positioned text blob.

The constructor defensively copies `glyphIds` and `glyphPositions`, and the `GlyphIds` and `GlyphPositions` accessors return copies. Mutating an input array or a returned array does not mutate the stored result.

The default value is safe to read: `Text` returns `string.Empty`, and the glyph arrays return empty arrays. A constructed value validates that `glyphCount` is non-negative, the glyph arrays match `glyphCount`, `advanceWidth` is finite and non-negative, and `originOffset` contains finite coordinates.

## Constructors

| Name | Description |
| --- | --- |
| `TextShapeResult(string text, int glyphCount)` | Initializes a result with zero-filled glyph id and position arrays whose lengths match `glyphCount`; `AdvanceWidth` is `0` and `OriginOffset` is `default`. |
| `TextShapeResult(string text, int glyphCount, ushort[] glyphIds, DrawPoint[] glyphPositions)` | Initializes a result with explicit glyph data; `AdvanceWidth` is `0` and `OriginOffset` is `default`. |
| `TextShapeResult(string text, int glyphCount, ushort[] glyphIds, DrawPoint[] glyphPositions, float advanceWidth)` | Initializes a result with explicit glyph data and total advance width; `OriginOffset` is `default`. |
| `TextShapeResult(string text, int glyphCount, ushort[] glyphIds, DrawPoint[] glyphPositions, float advanceWidth, DrawPoint originOffset)` | Initializes a result with explicit glyph data, total advance width, and origin offset. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Text` | `string` | Gets the text that was shaped. For the default value, returns `string.Empty`. |
| `GlyphCount` | `int` | Gets the number of shaped glyphs. |
| `AdvanceWidth` | `float` | Gets the total horizontal advance reported for the shaped glyph run. |
| `OriginOffset` | `DrawPoint` | Gets the offset from the shaped glyph positions to the rendered text origin. |
| `GlyphIds` | `ushort[]` | Gets a copy of the shaped glyph id array. For the default value, returns an empty array. |
| `GlyphPositions` | `DrawPoint[]` | Gets a copy of the shaped glyph position array. For the default value, returns an empty array. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| Constructors | `ArgumentNullException` | `text`, `glyphIds`, or `glyphPositions` is `null`. |
| Constructors | `ArgumentOutOfRangeException` | `glyphCount` is negative, the zero-filled array constructor cannot allocate the requested glyph count, `advanceWidth` is not finite or is negative, or `originOffset` contains a non-finite coordinate. |
| Constructors | `ArgumentException` | `glyphIds.Length` or `glyphPositions.Length` does not equal `glyphCount`. |

## Applies To

Cerneala drawing text pipeline.

## See Also

- `Cerneala.Drawing.Text.TextShaper`
- `Cerneala.Drawing.Text.SkiaTextShaper`
- `Cerneala.Drawing.Text.RasterizedText`
- `Cerneala.Drawing.DrawTextRun`
